package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"strings"
)

func (b *irBuilder) nativeStorageType(t *checker.Type, seen map[*checker.Type]bool) bool {
	if t == nil || seen[t] {
		return false
	}
	seen[t] = true
	if t.Flags()&(checker.TypeFlagsStringLike|checker.TypeFlagsNumberLike|checker.TypeFlagsBooleanLike|checker.TypeFlagsESSymbolLike|checker.TypeFlagsBigIntLike) != 0 {
		return true
	}
	if t.Flags()&checker.TypeFlagsTypeParameter != 0 {
		return b.nativeStorageType(b.c.GetBaseConstraintOfType(t), seen)
	}
	if t.Flags()&(checker.TypeFlagsUnion|checker.TypeFlagsIntersection) != 0 {
		for _, part := range t.Types() {
			if b.nativeStorageType(part, seen) {
				return true
			}
		}
		return false
	}
	if len(b.c.GetSignaturesOfType(t, checker.SignatureKindCall)) != 0 || len(b.c.GetConstructSignatures(t)) != 0 {
		return true
	}
	if nativeObjectType(t) || b.c.IsArrayType(t) || checker.IsTupleType(t) || b.arrayLikeType(t) || b.thenableType(t) || b.collectionInterfaceType(t) {
		return true
	}
	if t.ObjectFlags()&checker.ObjectFlagsReference != 0 {
		t = t.Target()
	}
	if t != nil && t.ObjectFlags()&checker.ObjectFlagsClassOrInterface != 0 {
		for _, base := range b.c.GetBaseTypes(t) {
			if b.nativeStorageType(base, seen) {
				return true
			}
		}
	}
	return false
}

// A native value cannot implement a compiler-generated CLR shape interface.
// Use object storage for the receiving TS view and propagate that choice through
// further conversions, before creating any IR types or field layouts.
func (b *irBuilder) prepareNativeShapeViews(files []*ast.SourceFile) {
	type edge struct{ source, target *checker.Type }
	var edges []edge
	seenConversions := map[edge]bool{}
	var targets func(*checker.Type, *checker.Type)
	targets = func(source, target *checker.Type) {
		if source == nil || target == nil || source == target || seenConversions[edge{source, target}] {
			return
		}
		seenConversions[edge{source, target}] = true
		if target.Flags()&checker.TypeFlagsTypeParameter != 0 {
			targets(source, b.c.GetBaseConstraintOfType(target))
			return
		}
		if target.Flags()&checker.TypeFlagsUnion != 0 {
			for _, part := range target.Types() {
				targets(source, part)
			}
			return
		}
		if target.Flags()&checker.TypeFlagsObject == 0 {
			return
		}
		if symbol := target.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsClass != 0 {
			return
		}
		if b.c.IsArrayType(target) || checker.IsTupleType(target) {
			if b.c.IsArrayType(source) || checker.IsTupleType(source) {
				a, z := b.c.GetTypeArguments(source), b.c.GetTypeArguments(target)
				if checker.IsTupleType(target) {
					for i, t := range z {
						if i < len(a) {
							targets(a[i], t)
						}
					}
				} else {
					for _, item := range a {
						if len(z) > 0 {
							targets(item, z[0])
						}
					}
				}
			}
			return
		}
		edges = append(edges, edge{source, target})
		if nativeObjectType(target) {
			if source.ObjectFlags()&checker.ObjectFlagsReference != 0 && target.ObjectFlags()&checker.ObjectFlagsReference != 0 {
				a, z := b.c.GetTypeArguments(source), b.c.GetTypeArguments(target)
				if len(a) == len(z) {
					for i, t := range z {
						targets(a[i], t)
					}
				}
			}
			return
		}
		for _, kind := range []checker.SignatureKind{checker.SignatureKindCall, checker.SignatureKindConstruct} {
			for _, a := range b.c.GetSignaturesOfType(source, kind) {
				for _, z := range b.c.GetSignaturesOfType(target, kind) {
					targets(b.c.GetReturnTypeOfSignature(a), b.c.GetReturnTypeOfSignature(z))
					ap, zp := a.Parameters(), z.Parameters()
					for i, p := range ap {
						if i < len(zp) {
							targets(b.c.GetTypeOfSymbol(zp[i]), b.c.GetTypeOfSymbol(p))
						}
					}
				}
			}
		}
		for _, property := range b.c.GetPropertiesOfType(target) {
			if checker.IsKnownSymbol(property) {
				continue
			}
			name := ast.SymbolName(property)
			if actual := b.c.GetTypeOfPropertyOfType(source, name); actual != nil {
				targets(actual, b.c.GetTypeOfPropertyOfType(target, name))
			}
		}
	}
	for _, file := range files {
		var visit func(*ast.Node) bool
		visit = func(n *ast.Node) bool {
			if n.Kind == ast.KindCallExpression && len(n.Arguments()) > 0 {
				if signature := b.c.GetResolvedSignature(n); signature != nil {
					declaration := signature.Declaration()
					if declaration != nil && declaration.Name() != nil && declaration.Parent != nil && declaration.Parent.Kind == ast.KindInterfaceDeclaration && declaration.Parent.Name().Text() == "ObjectConstructor" {
						file := ast.GetSourceFileOfNode(declaration)
						if file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
							switch declaration.Name().Text() {
							case "freeze", "seal", "preventExtensions", "defineProperty", "defineProperties":
								// These methods return the original object, including native objects
								// viewed through a mapped return type such as Readonly<Error>.
								targets(b.c.GetTypeAtLocation(n.Arguments()[0]), b.c.GetTypeAtLocation(n))
							}
						}
					}
				}
			}
			if n.Kind == ast.KindCallExpression || n.Kind == ast.KindNewExpression {
				if signature := b.c.GetResolvedSignature(n); signature != nil && signature.Target() != nil {
					parameters := signature.Target().Parameters()
					var arguments []*checker.Type
					for _, argument := range n.Arguments() {
						if argument.Kind != ast.KindSpreadElement {
							arguments = append(arguments, b.c.GetTypeAtLocation(argument))
							continue
						}
						source := b.c.GetTypeAtLocation(argument.Expression())
						if checker.IsTupleType(source) {
							parts := b.c.GetTypeArguments(source)
							for i := range parts {
								arguments = append(arguments, b.nativeRestElement(source, i, len(parts)-i-1))
							}
						} else {
							arguments = append(arguments, b.nativeRestElement(source, 0, 0))
						}
					}
					for i, source := range arguments {
						index := i
						rest := signature.Target().HasRestParameter() && len(parameters) > 0 && i >= len(parameters)-1
						if rest {
							index = len(parameters) - 1
						}
						if index >= len(parameters) {
							continue
						}
						parameter := b.c.GetTypeOfSymbol(parameters[index])
						if rest {
							parameter = b.nativeRestElement(parameter, i-index, len(arguments)-i-1)
						}
						targets(source, parameter)
					}
				}
			}
			if ast.IsExpressionNode(n) && (n.Parent == nil || n.Parent.Kind != ast.KindSatisfiesExpression) {
				if target := b.c.GetContextualType(n, checker.ContextFlagsNone); target != nil {
					targets(b.c.GetTypeAtLocation(n), target)
				}
			}
			return n.ForEachChild(visit)
		}
		visit(file.AsNode())
	}
	b.nativeShapeViews = map[*checker.Type]bool{}
	native := map[*checker.Type]bool{}
	for _, conversion := range edges {
		if _, known := native[conversion.source]; !known {
			native[conversion.source] = b.nativeStorageType(conversion.source, map[*checker.Type]bool{})
		}
	}

	graph := map[*checker.Type][]*checker.Type{}
	var queue []*checker.Type
	queued := map[*checker.Type]bool{}
	for _, conversion := range edges {
		graph[conversion.source] = append(graph[conversion.source], conversion.target)
		if native[conversion.source] && !queued[conversion.source] {
			queue = append(queue, conversion.source)
			queued[conversion.source] = true
		}
	}
	for position := 0; position < len(queue); position++ {
		for _, target := range graph[queue[position]] {
			b.nativeShapeViews[target] = true
			if !queued[target] {
				queued[target] = true
				queue = append(queue, target)
			}
		}
	}
}

// Tuple metadata distinguishes fixed parameters from repeated or variadic parts.
func (b *irBuilder) nativeRestElement(t *checker.Type, position, remaining int) *checker.Type {
	if t == nil {
		return nil
	}
	if t.Flags()&checker.TypeFlagsTypeParameter != 0 {
		t = b.c.GetBaseConstraintOfType(t)
		if t == nil {
			return nil
		}
	}
	if b.c.IsArrayType(t) {
		return b.c.GetElementTypeOfArrayType(t)
	}
	if !checker.IsTupleType(t) {
		return nil
	}
	parts := b.c.GetTypeArguments(t)
	flags := t.TargetTupleType().ElementFlags()
	index := position
	for i, flag := range flags {
		if flag&checker.ElementFlagsVariable != 0 && position >= i {
			tail := len(parts) - i - 1
			if remaining < tail {
				index = len(parts) - remaining - 1
			} else {
				index = i
			}
			break
		}
	}
	if index < 0 || index >= len(parts) {
		return nil
	}
	if flags[index]&checker.ElementFlagsVariadic != 0 {
		return b.nativeRestElement(parts[index], position-index, remaining-(len(parts)-index-1))
	}
	return parts[index]
}

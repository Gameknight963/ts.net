package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
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
			if n.Kind == ast.KindCallExpression || n.Kind == ast.KindNewExpression {
				if signature := b.c.GetResolvedSignature(n); signature != nil && signature.Target() != nil {
					parameters := signature.Target().Parameters()
					arguments := n.Arguments()
					if arguments != nil {
						for i, argument := range arguments {
							if i < len(parameters) {
								targets(b.c.GetTypeAtLocation(argument), b.c.GetTypeOfSymbol(parameters[i]))
							}
						}
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

package main

import (
	"maps"
	"strconv"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// The checker cannot provide a source signature for a call through any.
// Follow the function's possible values instead, using the same alias sources
// that property and reflection analysis already consume.
func (b *irBuilder) prepareIndirectReflectionSources(files []*ast.SourceFile) {
	var calls []*ast.Node
	var scan func(*ast.Node) bool
	scan = func(n *ast.Node) bool {
		if n.Kind == ast.KindCallExpression || n.Kind == ast.KindNewExpression {
			_, method := deletionAccess(n.Expression())
			indirect := b.c.GetTypeAtLocation(n.Expression()).Flags()&checker.TypeFlagsAny != 0 || method == "call" || method == "apply" || method == "bind"
			if !indirect {
				if signature := b.c.GetResolvedSignature(n); signature != nil {
					declaration := signature.Declaration()
					if declaration != nil && (!ast.IsFunctionLike(declaration) || declaration.Body() == nil) {
						file := ast.GetSourceFileOfNode(declaration)
						indirect = file != nil && !strings.HasPrefix(file.FileName(), "bundled:///libs/")
					}
				}
			}
			if indirect {
				calls = append(calls, n)
			}
		}
		return n.ForEachChild(scan)
	}
	for _, file := range files {
		scan(file.AsNode())
	}
	type edge struct{ parameter, value *ast.Node }
	known := map[edge]bool{}
	for parameter, sources := range b.reflectionSources {
		for _, source := range sources {
			known[edge{parameter, source}] = true
		}
	}
	changed := true
	connect := func(parameter, value *ast.Node) {
		if value != nil && parameter != nil && parameter.Kind == ast.KindParameter &&
			parameter.AsParameterDeclaration().DotDotDotToken == nil && !known[edge{parameter, value}] {
			known[edge{parameter, value}] = true
			b.reflectionSources[parameter] = append(b.reflectionSources[parameter], value)
			changed = true
		}
	}
	// An indirect forwarding call may reveal the source of another callee.
	// Edges only accumulate and are deduplicated, so cycles converge.
	for changed {
		changed = false
		for _, call := range calls {
			arguments := reflectionLiteralArguments(call.Arguments())
			bind := func(signature *checker.Signature, apply *ast.Node, values []*ast.Node) {
				if signature.Target() != nil {
					signature = signature.Target()
				}
				for index, parameter := range signature.Parameters() {
					if apply != nil {
						b.reflectionContainerValue(apply, []string{strconv.Itoa(index)}, map[*ast.Node]bool{}, func(value *ast.Node, _ map[*ast.Node]bool) int {
							connect(parameter.ValueDeclaration, value)
							return 0
						})
					} else {
						if index >= len(values) || values[index].Kind == ast.KindSpreadElement {
							break
						}
						connect(parameter.ValueDeclaration, values[index])
					}
				}
			}
			b.indirectSourceSignatures(call.Expression(), func(signature *checker.Signature) { bind(signature, nil, arguments) })
			owner, method := deletionAccess(call.Expression())
			if owner != nil && (method == "call" || method == "bind") && len(arguments) > 0 {
				b.indirectSourceSignatures(owner, func(signature *checker.Signature) { bind(signature, nil, arguments[1:]) })
			} else if owner != nil && method == "apply" && len(arguments) > 1 {
				b.indirectSourceSignatures(owner, func(signature *checker.Signature) { bind(signature, arguments[1], nil) })
			}
		}
	}
}

func (b *irBuilder) indirectSourceSignatures(source *ast.Node, found func(*checker.Signature)) {
	b.indirectSourceSignaturesSeen(source, map[*ast.Node]bool{}, found)
}

func (b *irBuilder) indirectSourceSignaturesSeen(source *ast.Node, active map[*ast.Node]bool, found func(*checker.Signature)) {
	var visit func(*ast.Node, map[*ast.Node]bool) int
	visit = func(n *ast.Node, seen map[*ast.Node]bool) int {
		if n == nil || seen[n] {
			return 0
		}
		seen[n] = true
		if ast.IsFunctionLike(n) && n.Body() != nil {
			if signature := b.c.GetSignatureFromDeclaration(n); signature != nil {
				found(signature)
			}
		}
		typ := b.c.GetTypeAtLocation(n)
		for _, signatures := range [][]*checker.Signature{b.c.GetSignaturesOfType(typ, checker.SignatureKindCall), b.c.GetConstructSignatures(typ)} {
			for _, signature := range signatures {
				if declaration := signature.Declaration(); declaration != nil && ast.IsFunctionLike(declaration) && declaration.Body() != nil {
					found(signature)
				}
			}
		}
		switch n.Kind {
		case ast.KindIdentifier:
			symbol := b.c.GetSymbolAtLocation(n)
			if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
				symbol = b.c.GetAliasedSymbol(symbol)
			}
			if symbol != nil && symbol.ValueDeclaration != nil {
				b.reflectionBindingValue(symbol.ValueDeclaration, seen, visit)
			}
			if symbol != nil {
				for _, declaration := range symbol.Declarations {
					if ast.IsFunctionLike(declaration) && declaration.Body() != nil {
						visit(declaration, maps.Clone(seen))
					}
				}
			}
		case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
			visit(n.Expression(), seen)
		case ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
			b.reflectionPropertyValueLimit(n, seen, visit, 3)
		case ast.KindConditionalExpression:
			visit(n.AsConditionalExpression().WhenTrue, maps.Clone(seen))
			visit(n.AsConditionalExpression().WhenFalse, seen)
		case ast.KindCallExpression:
			b.reflectionReturnedValue(n, seen, visit)
		}
		return 0
	}
	visit(source, active)
}

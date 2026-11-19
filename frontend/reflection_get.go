package main

import (
	"maps"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
)

// Resolve the library declaration so aliases work and unrelated get methods
// do not change object layouts.
func (b *irBuilder) reflectionGetCall(n *ast.Node, seen map[*ast.Node]bool) bool {
	if b.reflectionGetFunction(n.Expression(), maps.Clone(seen)) != 0 {
		return true
	}
	signature := b.c.GetResolvedSignature(n)
	if signature == nil {
		return false
	}
	if signature.Target() != nil {
		signature = signature.Target()
	}
	declaration := signature.Declaration()
	if declaration == nil || declaration.Name() == nil || declaration.Name().Text() != "get" {
		return false
	}
	file := ast.GetSourceFileOfNode(declaration)
	if file == nil || !strings.HasPrefix(file.FileName(), "bundled:///libs/") {
		return false
	}
	for owner := declaration.Parent; owner != nil; owner = owner.Parent {
		if owner.Kind == ast.KindModuleDeclaration && owner.Name() != nil && owner.Name().Text() == "Reflect" {
			return true
		}
	}
	return false
}

func (b *irBuilder) reflectionGetFunction(n *ast.Node, seen map[*ast.Node]bool) int {
	if n == nil || seen[n] {
		return 0
	}
	seen[n] = true
	switch n.Kind {
	case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
		return b.reflectionGetFunction(n.Expression(), seen)
	case ast.KindIdentifier:
		symbol := b.c.GetSymbolAtLocation(n)
		if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
			symbol = b.c.GetAliasedSymbol(symbol)
		}
		if symbol != nil && symbol.ValueDeclaration != nil {
			return b.reflectionBindingValue(symbol.ValueDeclaration, seen, b.reflectionGetFunction)
		}
	case ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
		owner, name := deletionAccess(n)
		if owner != nil && name == "get" && b.reflectionDeletionValue(owner, maps.Clone(seen)) == 1 {
			return 1
		}
		return b.reflectionPropertyValueLimit(n, seen, b.reflectionGetFunction, 1)
	case ast.KindCallExpression:
		owner, name := deletionAccess(n.Expression())
		if owner != nil && name == "bind" && len(n.Arguments()) <= 1 && b.reflectionGetFunction(owner, maps.Clone(seen)) != 0 {
			return 1
		}
		return b.reflectionReturnedValue(n, seen, b.reflectionGetFunction)
	case ast.KindConditionalExpression:
		expression := n.AsConditionalExpression()
		return max(b.reflectionGetFunction(expression.WhenTrue, maps.Clone(seen)), b.reflectionGetFunction(expression.WhenFalse, seen))
	}
	return 0
}

func (b *irBuilder) reflectionGetValue(n *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int, methods ...string) (int, bool) {
	if !b.reflectionGetCall(n, seen) {
		return 0, false
	}
	arguments := reflectionLiteralArguments(n.Arguments())
	if len(arguments) < 2 || arguments[0].Kind == ast.KindSpreadElement || arguments[1].Kind == ast.KindSpreadElement {
		return 3, true
	}
	owner := arguments[0]
	names, known := b.constantDeletionKeys(arguments[1])
	if classify(owner, maps.Clone(seen)) == 1 {
		if !known {
			return 3, true
		}
		for _, name := range names {
			for _, method := range methods {
				if name == method {
					return 2, true
				}
			}
		}
		return 0, true
	}
	if !known {
		names = nil
		for _, property := range b.c.GetPropertiesOfType(b.c.GetTypeAtLocation(owner)) {
			names = append(names, ast.SymbolName(property))
		}
	}
	return b.reflectionReadProperties(owner, names, !known, seen, classify, 3), true
}

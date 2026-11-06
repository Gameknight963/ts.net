package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// Fold only syntax without reads or calls. A literal-typed identifier can still
// throw before its binding initializes and must be evaluated at runtime.
func (b *irBuilder) literalPropertyName(name *ast.Node) (string, bool) {
	if name.Kind == ast.KindIdentifier || name.Kind == ast.KindStringLiteral || name.Kind == ast.KindNumericLiteral {
		return name.Text(), true
	}
	if name.Kind != ast.KindComputedPropertyName {
		return "", false
	}
	key := name.Expression()
	for key.Kind == ast.KindParenthesizedExpression {
		key = key.Expression()
	}
	pure := key.Kind == ast.KindStringLiteral || key.Kind == ast.KindNoSubstitutionTemplateLiteral || key.Kind == ast.KindNumericLiteral
	if key.Kind == ast.KindPrefixUnaryExpression && key.AsPrefixUnaryExpression().Operand.Kind == ast.KindNumericLiteral {
		pure = true
	}
	if pure {
		typ := b.c.GetTypeAtLocation(key)
		if typ.Flags()&(checker.TypeFlagsStringLiteral|checker.TypeFlagsNumberLiteral) != 0 {
			return checker.GetPropertyNameFromType(typ), true
		}
	}
	return "", false
}

func anonymousPropertyFunction(n *ast.Node) bool {
	if n == nil {
		return false
	}
	for n.Kind == ast.KindParenthesizedExpression || n.Kind == ast.KindAsExpression || n.Kind == ast.KindTypeAssertionExpression || n.Kind == ast.KindSatisfiesExpression {
		n = n.Expression()
	}
	return n.Kind == ast.KindArrowFunction || n.Kind == ast.KindFunctionExpression && n.Name() == nil
}

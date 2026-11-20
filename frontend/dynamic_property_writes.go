package main

import (
	"maps"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// An any alias can replace a field with a different kind of value. Keep only
// the affected literals in dynamic storage; compatible numeric writes must not
// change the layout of unrelated typed records.
func (b *irBuilder) prepareDynamicPropertyWrite(n *ast.Node) {
	var left *ast.Node
	var value *checker.Type
	switch n.Kind {
	case ast.KindBinaryExpression:
		expression := n.AsBinaryExpression()
		if !ast.IsAssignmentOperator(expression.OperatorToken.Kind) {
			return
		}
		left = expression.Left
		value = b.c.GetTypeAtLocation(expression.Right)
		if expression.OperatorToken.Kind != ast.KindEqualsToken {
			value = b.c.GetTypeAtLocation(n)
		}
	case ast.KindPrefixUnaryExpression:
		expression := n.AsPrefixUnaryExpression()
		if expression.Operator != ast.KindPlusPlusToken && expression.Operator != ast.KindMinusMinusToken {
			return
		}
		left, value = expression.Operand, b.c.GetTypeAtLocation(n)
	case ast.KindPostfixUnaryExpression:
		left, value = n.AsPostfixUnaryExpression().Operand, b.c.GetTypeAtLocation(n)
	default:
		return
	}
	for left.Kind == ast.KindParenthesizedExpression {
		left = left.Expression()
	}
	if left.Kind != ast.KindPropertyAccessExpression && left.Kind != ast.KindElementAccessExpression {
		return
	}
	receiver := left.Expression()
	if b.c.GetTypeAtLocation(receiver).Flags()&checker.TypeFlagsAny == 0 {
		return
	}
	var names []string
	known := true
	if left.Kind == ast.KindPropertyAccessExpression {
		names = []string{left.Name().Text()}
	} else {
		names, known = b.constantDeletionKeys(left.AsElementAccessExpression().ArgumentExpression)
	}
	var visit func(*ast.Node, map[*ast.Node]bool) int
	visit = func(source *ast.Node, seen map[*ast.Node]bool) int {
		if source == nil || seen[source] {
			return 0
		}
		seen[source] = true
		switch source.Kind {
		case ast.KindObjectLiteralExpression:
			typ := b.c.GetTypeAtLocation(source)
			properties := names
			if !known {
				properties = nil
				for _, property := range b.c.GetPropertiesOfType(typ) {
					properties = append(properties, ast.SymbolName(property))
				}
			}
			for _, name := range properties {
				field := b.c.GetTypeOfPropertyOfType(typ, name)
				// any is assignable to every TS type, but that does not prove
				// that its runtime value fits a fixed CLR field.
				if field != nil && field.Flags()&(checker.TypeFlagsAny|checker.TypeFlagsUnknown) == 0 && (value.Flags()&checker.TypeFlagsAny != 0 || !b.c.IsTypeAssignableTo(value, field)) {
					b.dynamicWriteLiterals[source] = true
				}
			}
		case ast.KindIdentifier:
			symbol := b.c.GetSymbolAtLocation(source)
			if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
				symbol = b.c.GetAliasedSymbol(symbol)
			}
			if symbol != nil && symbol.ValueDeclaration != nil {
				b.reflectionBindingValue(symbol.ValueDeclaration, seen, visit)
			}
		case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
			visit(source.Expression(), seen)
		case ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
			b.reflectionPropertyValueLimit(source, seen, visit, 3)
		case ast.KindCallExpression:
			b.reflectionReturnedValue(source, seen, visit)
		case ast.KindConditionalExpression:
			visit(source.AsConditionalExpression().WhenTrue, maps.Clone(seen))
			visit(source.AsConditionalExpression().WhenFalse, seen)
		case ast.KindBinaryExpression:
			binary := source.AsBinaryExpression()
			if binary.OperatorToken.Kind != ast.KindEqualsToken && binary.OperatorToken.Kind != ast.KindCommaToken {
				visit(binary.Left, maps.Clone(seen))
			}
			visit(binary.Right, seen)
		case ast.KindAwaitExpression:
			b.reflectionAwaitedValue(source.Expression(), seen, visit)
		}
		return 0
	}
	visit(receiver, map[*ast.Node]bool{})
}

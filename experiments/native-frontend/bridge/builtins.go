package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"strings"
)

func (b *irBuilder) isBuiltinHasOwn(n *ast.Node) bool {
	if n.Kind != ast.KindPropertyAccessExpression || n.Name().Text() != "hasOwn" || n.Expression().Kind != ast.KindIdentifier || n.Expression().Text() != "Object" {
		return false
	}
	symbol := b.c.GetSymbolAtLocation(n.Expression())
	if symbol == nil || symbol.ValueDeclaration == nil {
		return false
	}
	file := ast.GetSourceFileOfNode(symbol.ValueDeclaration)
	return file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/")
}

func (b *irBuilder) isBuiltinIdentifier(n *ast.Node) bool {
	symbol := b.c.GetSymbolAtLocation(n)
	if n.Parent != nil && n.Parent.Kind == ast.KindShorthandPropertyAssignment {
		symbol = b.c.GetShorthandAssignmentValueSymbol(n.Parent)
	}
	if symbol == nil || symbol.ValueDeclaration == nil {
		return false
	}
	file := ast.GetSourceFileOfNode(symbol.ValueDeclaration)
	return file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/")
}

func identifierWrite(n *ast.Node) bool {
	for n.Parent != nil && n.Parent.Kind == ast.KindParenthesizedExpression {
		n = n.Parent
	}
	parent := n.Parent
	if parent == nil {
		return false
	}
	switch parent.Kind {
	case ast.KindBinaryExpression:
		expression := parent.AsBinaryExpression()
		return expression.Left == n && expression.OperatorToken.Kind >= ast.KindFirstAssignment && expression.OperatorToken.Kind <= ast.KindLastAssignment
	case ast.KindPrefixUnaryExpression:
		operator := parent.AsPrefixUnaryExpression().Operator
		return operator == ast.KindPlusPlusToken || operator == ast.KindMinusMinusToken
	case ast.KindPostfixUnaryExpression:
		return true
	}
	return false
}

func (b *irBuilder) numberPredicate(n *ast.Node) int {
	for n.Kind == ast.KindParenthesizedExpression {
		n = n.Expression()
	}
	var owner *ast.Node
	name := ""
	switch n.Kind {
	case ast.KindPropertyAccessExpression:
		owner, name = n.Expression(), n.Name().Text()
	case ast.KindElementAccessExpression:
		expression := n.AsElementAccessExpression()
		if expression.ArgumentExpression.Kind != ast.KindStringLiteral {
			return -1
		}
		owner, name = expression.Expression, expression.ArgumentExpression.Text()
	default:
		return -1
	}
	if owner.Kind != ast.KindIdentifier || owner.Text() != "Number" || !b.isBuiltinIdentifier(owner) {
		return -1
	}
	switch name {
	case "isNaN":
		return 0
	case "isFinite":
		return 1
	case "isInteger":
		return 2
	case "isSafeInteger":
		return 3
	}
	return -1
}

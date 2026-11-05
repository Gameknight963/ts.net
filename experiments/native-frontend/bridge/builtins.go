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

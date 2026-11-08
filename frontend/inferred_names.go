package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) inferredExpressionName(n *ast.Node) string {
	child, parent := n, n.Parent
	for parent != nil {
		switch parent.Kind {
		case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindSatisfiesExpression, ast.KindNonNullExpression:
			child, parent = parent, parent.Parent
		default:
			goto context
		}
	}
context:
	if parent == nil {
		return ""
	}
	switch parent.Kind {
	case ast.KindVariableDeclaration, ast.KindPropertyAssignment, ast.KindPropertyDeclaration, ast.KindParameter, ast.KindBindingElement, ast.KindShorthandPropertyAssignment:
		if name := parent.Name(); name != nil {
			if name.Kind == ast.KindPrivateIdentifier {
				return name.Text()
			}
			if value, known := b.literalPropertyName(name); known {
				return value
			}
		}
	case ast.KindBinaryExpression:
		binary := parent.AsBinaryExpression()
		if binary.Right != child {
			return ""
		}
		switch binary.OperatorToken.Kind {
		case ast.KindEqualsToken, ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
			left := binary.Left
			if left.Kind == ast.KindIdentifier {
				return left.Text()
			}
		}
	case ast.KindExportAssignment:
		if !parent.AsExportAssignment().IsExportEquals {
			return "default"
		}
	}
	return ""
}

package main

import (
	"maps"

	"github.com/microsoft/typescript-go/internal/ast"
)

func (b *irBuilder) reflectionDefinitionArguments(n *ast.Node) []*ast.Node {
	callee, arguments := n.Expression(), n.AsCallExpression().Arguments.Nodes
	if b.reflectionDefinitionValue(callee, map[*ast.Node]bool{}) >= 2 {
		return arguments
	}
	owner, method := deletionAccess(callee)
	if owner == nil || method != "call" && method != "apply" || b.reflectionDefinitionValue(owner, map[*ast.Node]bool{}) < 2 {
		return nil
	}
	if method == "call" && len(arguments) >= 1 {
		return arguments[1:]
	}
	if method == "apply" && len(arguments) >= 2 && arguments[1].Kind == ast.KindArrayLiteralExpression {
		items := arguments[1].AsArrayLiteralExpression().Elements.Nodes
		for _, item := range items {
			if item.Kind == ast.KindSpreadElement || item.Kind == ast.KindOmittedExpression {
				return nil
			}
		}
		return items
	}
	return nil
}

func (b *irBuilder) reflectionDefinitionCallKind(n *ast.Node) int {
	callee := n.Expression()
	if kind := b.reflectionDefinitionValue(callee, map[*ast.Node]bool{}); kind >= 2 {
		return kind
	}
	owner, method := deletionAccess(callee)
	if owner == nil {
		return 0
	}
	kind := b.reflectionDefinitionValue(owner, map[*ast.Node]bool{})
	arguments := n.AsCallExpression().Arguments.Nodes
	if kind >= 2 && (method == "call" || method == "apply" || method == "bind" && len(arguments) >= 2) {
		return kind
	}
	if kind == 1 && method == "apply" && len(arguments) >= 1 && b.reflectionDefinitionValue(arguments[0], map[*ast.Node]bool{}) >= 2 {
		return 3
	}
	return 0
}

// Values are 1 for Object/Reflect, 2 for a defining method, and 3 for an unknown method.
// Merely retaining namespace or method references must not change object layouts.
func (b *irBuilder) reflectionDefinitionValue(n *ast.Node, seen map[*ast.Node]bool) int {
	if n == nil || seen[n] {
		return 0
	}
	seen[n] = true
	switch n.Kind {
	case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
		return b.reflectionDefinitionValue(n.Expression(), seen)
	case ast.KindIdentifier:
		if (n.Text() == "Object" || n.Text() == "Reflect") && b.isBuiltinIdentifier(n) {
			return 1
		}
		symbol := b.c.GetSymbolAtLocation(n)
		if symbol == nil {
			return 0
		}
		if symbol.Flags&ast.SymbolFlagsAlias != 0 {
			symbol = b.c.GetAliasedSymbol(symbol)
		}
		if symbol == nil || symbol.ValueDeclaration == nil {
			return 0
		}
		declaration := symbol.ValueDeclaration
		if declaration.Kind == ast.KindParameter || declaration.Kind == ast.KindVariableDeclaration {
			return b.reflectionBindingValue(declaration, seen, b.reflectionDefinitionValue)
		}
		if declaration.Kind == ast.KindBindingElement && declaration.Parent.Kind == ast.KindObjectBindingPattern {
			name := declaration.AsBindingElement().PropertyName
			if name == nil {
				name = declaration.Name()
			}
			if name.Kind == ast.KindComputedPropertyName {
				name = name.Expression()
			}
			owner := declaration.Parent.Parent
			if (name.Kind == ast.KindIdentifier || name.Kind == ast.KindStringLiteral) && (name.Text() == "defineProperty" || name.Text() == "defineProperties" || name.Text() == "preventExtensions" || name.Text() == "seal" || name.Text() == "freeze") && owner != nil &&
				(owner.Kind == ast.KindVariableDeclaration || owner.Kind == ast.KindParameter) && b.reflectionBindingValue(owner, seen, b.reflectionDefinitionValue) == 1 {
				return 2
			}
		}
		if declaration.Kind == ast.KindBindingElement {
			return b.reflectionBindingValue(declaration, seen, b.reflectionDefinitionValue)
		}
	case ast.KindCallExpression:
		owner, method := deletionAccess(n.Expression())
		if method == "bind" && len(n.AsCallExpression().Arguments.Nodes) <= 1 {
			if kind := b.reflectionDefinitionValue(owner, seen); kind >= 2 {
				return kind
			}
		}
		return b.reflectionReturnedValue(n, seen, b.reflectionDefinitionValue)
	case ast.KindBinaryExpression:
		expression := n.AsBinaryExpression()
		if expression.OperatorToken.Kind == ast.KindCommaToken || expression.OperatorToken.Kind == ast.KindEqualsToken {
			return b.reflectionDefinitionValue(expression.Right, seen)
		}
		switch expression.OperatorToken.Kind {
		case ast.KindAmpersandAmpersandToken, ast.KindBarBarToken, ast.KindQuestionQuestionToken,
			ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
			left := b.reflectionDefinitionValue(expression.Left, seen)
			right := b.reflectionDefinitionValue(expression.Right, seen)
			if right > left {
				return right
			}
			return left
		}
	case ast.KindConditionalExpression:
		expression := n.AsConditionalExpression()
		left := b.reflectionDefinitionValue(expression.WhenTrue, seen)
		right := b.reflectionDefinitionValue(expression.WhenFalse, seen)
		if right > left {
			return right
		}
		return left
	case ast.KindPropertyAccessExpression:
		if (n.Name().Text() == "defineProperty" || n.Name().Text() == "defineProperties" || n.Name().Text() == "preventExtensions" || n.Name().Text() == "seal" || n.Name().Text() == "freeze") && b.reflectionDefinitionValue(n.Expression(), maps.Clone(seen)) == 1 {
			return 2
		}
		return b.reflectionPropertyValueLimit(n, seen, b.reflectionDefinitionValue, 3)
	case ast.KindElementAccessExpression:
		key := n.AsElementAccessExpression().ArgumentExpression
		if b.reflectionDefinitionValue(n.Expression(), maps.Clone(seen)) == 1 {
			if key.Kind == ast.KindStringLiteral {
				switch key.Text() {
				case "defineProperty", "defineProperties", "preventExtensions", "seal", "freeze":
					return 2
				}
				return 0
			}
			return 3
		}
		return b.reflectionPropertyValueLimit(n, seen, b.reflectionDefinitionValue, 3)
	}
	return 0
}

func (b *irBuilder) prepareDefinition(n *ast.Node) {
	kind := b.reflectionDefinitionCallKind(n)
	if kind < 2 {
		return
	}
	if kind >= 3 {
		b.deleteAnyName = true
		return
	}
	arguments := b.reflectionDefinitionArguments(n)
	if kind >= 2 && len(arguments) == 0 {
		_, method := deletionAccess(n.Expression())
		if method == "apply" {
			b.deleteAnyName = true
		}
	}
	if len(arguments) < 1 {
		owner, method := deletionAccess(n.Expression())
		bound := n.AsCallExpression().Arguments.Nodes
		if method != "bind" || len(bound) < 2 || b.reflectionDefinitionValue(owner, map[*ast.Node]bool{}) < 2 {
			return
		}
		arguments = bound[1:]
	}
	properties := b.c.GetPropertiesOfType(b.c.GetTypeAtLocation(arguments[0]))
	if len(properties) == 0 {
		b.deleteAnyName = true
	}
	for _, property := range properties {
		b.deletedNames[ast.SymbolName(property)] = true
	}
}

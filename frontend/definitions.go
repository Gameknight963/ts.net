package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) reflectionDefinitionArguments(n *ast.Node) []*ast.Node {
	callee, arguments := n.Expression(), n.AsCallExpression().Arguments.Nodes
	if b.reflectionDefinitionValue(callee, map[*ast.Node]bool{}) == 2 {
		return arguments
	}
	owner, method := deletionAccess(callee)
	if owner == nil || method != "call" && method != "apply" || b.reflectionDefinitionValue(owner, map[*ast.Node]bool{}) != 2 {
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

// Follow Object.defineProperty aliases without changing layouts for read-only Object use.
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
		if declaration.Kind == ast.KindVariableDeclaration {
			return b.reflectionDefinitionValue(declaration.Initializer(), seen)
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
				(owner.Kind == ast.KindVariableDeclaration || owner.Kind == ast.KindParameter) && b.reflectionDefinitionValue(owner.Initializer(), seen) == 1 {
				return 2
			}
		}
	case ast.KindCallExpression:
		owner, method := deletionAccess(n.Expression())
		if method == "bind" && len(n.AsCallExpression().Arguments.Nodes) <= 1 && b.reflectionDefinitionValue(owner, seen) == 2 {
			return 2
		}
	case ast.KindBinaryExpression:
		expression := n.AsBinaryExpression()
		if expression.OperatorToken.Kind == ast.KindCommaToken {
			return b.reflectionDefinitionValue(expression.Right, seen)
		}
	case ast.KindConditionalExpression:
		expression := n.AsConditionalExpression()
		left := b.reflectionDefinitionValue(expression.WhenTrue, seen)
		right := b.reflectionDefinitionValue(expression.WhenFalse, seen)
		if left == 2 || right == 2 {
			return 2
		}
		if left == 1 || right == 1 {
			return 1
		}
	case ast.KindPropertyAccessExpression:
		if (n.Name().Text() == "defineProperty" || n.Name().Text() == "defineProperties" || n.Name().Text() == "preventExtensions" || n.Name().Text() == "seal" || n.Name().Text() == "freeze") && b.reflectionDefinitionValue(n.Expression(), seen) == 1 {
			return 2
		}
	case ast.KindElementAccessExpression:
		key := n.AsElementAccessExpression().ArgumentExpression
		if key.Kind == ast.KindStringLiteral && (key.Text() == "defineProperty" || key.Text() == "defineProperties" || key.Text() == "preventExtensions" || key.Text() == "seal" || key.Text() == "freeze") && b.reflectionDefinitionValue(n.Expression(), seen) == 1 {
			return 2
		}
	}
	return 0
}

func (b *irBuilder) prepareDefinition(n *ast.Node) {
	arguments := b.reflectionDefinitionArguments(n)
	if len(arguments) < 1 {
		owner, method := deletionAccess(n.Expression())
		bound := n.AsCallExpression().Arguments.Nodes
		if method != "bind" || len(bound) < 2 || b.reflectionDefinitionValue(owner, map[*ast.Node]bool{}) != 2 {
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

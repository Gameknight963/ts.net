package main

import (
	"maps"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
)

// Follow fulfilled values only when an await consumes them. Merely storing a
// promise containing a reflection method must not change unrelated layouts.
func (b *irBuilder) reflectionAwaitedValue(n *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	var resolve func(*ast.Node, map[*ast.Node]bool) int
	resolve = func(n *ast.Node, active map[*ast.Node]bool) int {
		if n == nil || active[n] {
			return 0
		}
		result := classify(n, maps.Clone(active))
		active = maps.Clone(active)
		active[n] = true
		add := func(value int) {
			if result == 1 && value >= 2 || value == 1 && result >= 2 {
				result = 3
			} else if value > result {
				result = value
			}
		}
		switch n.Kind {
		case ast.KindAwaitExpression, ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
			add(resolve(n.Expression(), active))
		case ast.KindIdentifier:
			symbol := b.c.GetSymbolAtLocation(n)
			if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
				symbol = b.c.GetAliasedSymbol(symbol)
			}
			if symbol != nil && symbol.ValueDeclaration != nil {
				declaration := symbol.ValueDeclaration
				if declaration.Kind == ast.KindVariableDeclaration || declaration.Kind == ast.KindParameter || declaration.Kind == ast.KindBindingElement {
					add(b.reflectionBindingValue(declaration, active, resolve))
				}
			}
		case ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
			add(b.reflectionPropertyValueLimit(n, active, resolve, 3))
		case ast.KindCallExpression:
			if b.reflectionPromiseResolve(n) {
				arguments := reflectionLiteralArguments(n.AsCallExpression().Arguments.Nodes)
				if len(arguments) > 0 && arguments[0].Kind != ast.KindSpreadElement {
					add(resolve(arguments[0], active))
				}
			} else {
				add(b.reflectionReturnedValue(n, active, resolve))
			}
		case ast.KindConditionalExpression:
			add(resolve(n.AsConditionalExpression().WhenTrue, active))
			add(resolve(n.AsConditionalExpression().WhenFalse, active))
		case ast.KindBinaryExpression:
			expression := n.AsBinaryExpression()
			switch expression.OperatorToken.Kind {
			case ast.KindCommaToken, ast.KindEqualsToken:
				add(resolve(expression.Right, active))
			case ast.KindAmpersandAmpersandToken, ast.KindBarBarToken, ast.KindQuestionQuestionToken,
				ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
				add(resolve(expression.Left, active))
				add(resolve(expression.Right, active))
			}
		}
		return result
	}
	return resolve(n, seen)
}

func (b *irBuilder) reflectionPromiseResolve(n *ast.Node) bool {
	signature := b.c.GetResolvedSignature(n)
	if signature == nil {
		return false
	}
	if signature.Target() != nil {
		signature = signature.Target()
	}
	declaration := signature.Declaration()
	if declaration == nil || declaration.Parent == nil || declaration.Name() == nil {
		return false
	}
	file := ast.GetSourceFileOfNode(declaration)
	if file == nil || !strings.HasPrefix(file.FileName(), "bundled:///libs/") {
		return false
	}
	owner := declaration.Parent
	return owner.Kind == ast.KindInterfaceDeclaration && owner.Name() != nil && owner.Name().Text() == "PromiseConstructor" && declaration.Name().Text() == "resolve"
}

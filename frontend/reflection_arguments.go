package main

import "github.com/microsoft/typescript-go/internal/ast"

// Retain expressions rather than their current classification: aliases can
// be reassigned or pass through several functions before reaching an operation.
func (b *irBuilder) prepareReflectionSources(files []*ast.SourceFile) {
	b.reflectionSources = map[*ast.Node][]*ast.Node{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindBinaryExpression {
			expression := n.AsBinaryExpression()
			switch expression.OperatorToken.Kind {
			case ast.KindEqualsToken, ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
				left := expression.Left
				for left.Kind == ast.KindParenthesizedExpression || left.Kind == ast.KindAsExpression || left.Kind == ast.KindNonNullExpression || left.Kind == ast.KindTypeAssertionExpression {
					left = left.Expression()
				}
				if left.Kind == ast.KindIdentifier {
					if symbol := b.c.GetSymbolAtLocation(left); symbol != nil && symbol.ValueDeclaration != nil {
						declaration := symbol.ValueDeclaration
						b.reflectionSources[declaration] = append(b.reflectionSources[declaration], expression.Right)
					}
				}
			}
		}
		if n.Kind == ast.KindCallExpression || n.Kind == ast.KindNewExpression {
			if signature := b.c.GetResolvedSignature(n); signature != nil {
				declaration := signature.Declaration()
				if declaration != nil && ast.IsFunctionLike(declaration) && declaration.Body() != nil {
					if signature.Target() != nil {
						signature = signature.Target()
					}
					parameters := signature.Parameters()
					for index, argument := range reflectionLiteralArguments(n.Arguments()) {
						// A spread changes subsequent positions; rest parameters hold
						// arrays, not individual callback or namespace values.
						if argument.Kind == ast.KindSpreadElement || index >= len(parameters) {
							break
						}
						parameter := parameters[index].ValueDeclaration
						if parameter != nil && parameter.Kind == ast.KindParameter && parameter.AsParameterDeclaration().DotDotDotToken == nil {
							b.reflectionSources[parameter] = append(b.reflectionSources[parameter], argument)
						}
					}
				}
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}

// Expand fresh array literals without executing their elements. Unknown
// spreads remain barriers because subsequent parameter positions are unknown.
func reflectionLiteralArguments(arguments []*ast.Node) []*ast.Node {
	expands := false
	for _, argument := range arguments {
		if argument.Kind == ast.KindSpreadElement {
			expands = true
			break
		}
	}
	if !expands {
		return arguments
	}
	var result []*ast.Node
	for _, argument := range arguments {
		if argument.Kind != ast.KindSpreadElement {
			result = append(result, argument)
			continue
		}
		value := argument.Expression()
		for value.Kind == ast.KindParenthesizedExpression || value.Kind == ast.KindAsExpression || value.Kind == ast.KindSatisfiesExpression || value.Kind == ast.KindTypeAssertionExpression || value.Kind == ast.KindNonNullExpression {
			value = value.Expression()
		}
		if value.Kind == ast.KindArrayLiteralExpression {
			result = append(result, reflectionLiteralArguments(value.AsArrayLiteralExpression().Elements.Nodes)...)
		} else {
			result = append(result, argument)
		}
	}
	return result
}

func (b *irBuilder) reflectionBindingValue(parameter *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	if seen[parameter] {
		return 0
	}
	seen[parameter] = true
	result := classify(parameter.Initializer(), seen)
	for _, argument := range b.reflectionSources[parameter] {
		if value := classify(argument, seen); value > result {
			result = value
		}
	}
	return result
}

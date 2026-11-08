package main

import "github.com/microsoft/typescript-go/internal/ast"

// Retain argument expressions rather than their current classification: a
// callback may pass through several functions before reaching an operation.
func (b *irBuilder) prepareReflectionArguments(files []*ast.SourceFile) {
	b.reflectionArguments = map[*ast.Node][]*ast.Node{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindCallExpression || n.Kind == ast.KindNewExpression {
			if signature := b.c.GetResolvedSignature(n); signature != nil {
				declaration := signature.Declaration()
				if declaration != nil && ast.IsFunctionLike(declaration) && declaration.Body() != nil {
					if signature.Target() != nil {
						signature = signature.Target()
					}
					parameters := signature.Parameters()
					for index, argument := range n.Arguments() {
						// A spread changes subsequent positions; rest parameters hold
						// arrays, not individual callback or namespace values.
						if argument.Kind == ast.KindSpreadElement || index >= len(parameters) {
							break
						}
						parameter := parameters[index].ValueDeclaration
						if parameter != nil && parameter.Kind == ast.KindParameter && parameter.AsParameterDeclaration().DotDotDotToken == nil {
							b.reflectionArguments[parameter] = append(b.reflectionArguments[parameter], argument)
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

func (b *irBuilder) reflectionParameterValue(parameter *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	if seen[parameter] {
		return 0
	}
	seen[parameter] = true
	result := classify(parameter.Initializer(), seen)
	for _, argument := range b.reflectionArguments[parameter] {
		if value := classify(argument, seen); value > result {
			result = value
		}
	}
	return result
}

func (b *irBuilder) reflectionBindingValue(owner *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	if owner.Kind == ast.KindParameter {
		return b.reflectionParameterValue(owner, seen, classify)
	}
	return classify(owner.Initializer(), seen)
}

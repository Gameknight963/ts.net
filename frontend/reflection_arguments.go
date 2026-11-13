package main

import (
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
)

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
				location := left
				if left.Kind == ast.KindPropertyAccessExpression {
					location = left.Name()
				}
				if left.Kind == ast.KindIdentifier || left.Kind == ast.KindPropertyAccessExpression || left.Kind == ast.KindElementAccessExpression {
					if symbol := b.c.GetSymbolAtLocation(location); symbol != nil && symbol.ValueDeclaration != nil {
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
	result := 0
	switch parameter.Kind {
	case ast.KindParameter, ast.KindVariableDeclaration, ast.KindPropertyAssignment, ast.KindPropertyDeclaration:
		result = classify(parameter.Initializer(), seen)
	case ast.KindBindingElement:
		result = classify(parameter.Initializer(), seen)
		binding := parameter.AsBindingElement()
		if binding.DotDotDotToken == nil {
			var names []string
			pattern := parameter.Parent
			if pattern.Kind == ast.KindObjectBindingPattern {
				name := binding.PropertyName
				if name == nil {
					name = parameter.Name()
				}
				computed := name.Kind == ast.KindComputedPropertyName
				if computed {
					name = name.Expression()
				}
				if name.Kind == ast.KindIdentifier && !computed || name.Kind == ast.KindStringLiteral {
					names = []string{name.Text()}
				}
			} else if pattern.Kind == ast.KindArrayBindingPattern {
				for index, item := range pattern.AsBindingPattern().Elements.Nodes {
					if item == parameter {
						names = []string{strconv.Itoa(index)}
						break
					}
				}
			}
			owner := pattern.Parent
			if owner != nil && (owner.Kind == ast.KindVariableDeclaration || owner.Kind == ast.KindParameter) {
				if value := b.reflectionContainerValue(owner.Initializer(), names, seen, classify); value > result {
					result = value
				}
				for _, source := range b.reflectionSources[owner] {
					if value := b.reflectionContainerValue(source, names, seen, classify); value > result {
						result = value
					}
				}
			}
		}
	case ast.KindGetAccessor:
		result = b.reflectionBodyValue(parameter.Body(), seen, classify)
	case ast.KindShorthandPropertyAssignment:
		if symbol := b.c.GetShorthandAssignmentValueSymbol(parameter); symbol != nil && symbol.ValueDeclaration != nil {
			result = b.reflectionBindingValue(symbol.ValueDeclaration, seen, classify)
		}
	}
	for _, argument := range b.reflectionSources[parameter] {
		if value := classify(argument, seen); value > result {
			result = value
		}
	}
	return result
}

// A method's storage location does not change whether it can mutate layouts.
// Track declarations and later assignments rather than relying on the member name.
func (b *irBuilder) reflectionPropertyValue(n *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	return b.reflectionPropertyValueLimit(n, seen, classify, 2)
}

func (b *irBuilder) reflectionPropertyValueLimit(n *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int, maximum int) int {
	var names []string
	unknown := false
	if n.Kind == ast.KindPropertyAccessExpression {
		names = []string{n.Name().Text()}
	} else if key := n.AsElementAccessExpression().ArgumentExpression; key.Kind == ast.KindStringLiteral || key.Kind == ast.KindNumericLiteral {
		names = []string{key.Text()}
	} else {
		unknown = true
		for _, property := range b.c.GetPropertiesOfType(b.c.GetTypeAtLocation(n.Expression())) {
			names = append(names, ast.SymbolName(property))
		}
	}
	result := 0
	for _, name := range names {
		property := b.c.GetPropertyOfType(b.c.GetTypeAtLocation(n.Expression()), name)
		if property == nil || property.ValueDeclaration == nil {
			continue
		}
		if value := b.reflectionBindingValue(property.ValueDeclaration, seen, classify); value > result {
			result = value
		}
	}
	if result < maximum {
		selected := names
		if unknown {
			selected = nil
		}
		if value := b.reflectionContainerValue(n.Expression(), selected, seen, classify); value > result {
			result = value
		}
	}
	return result
}

// Follow container initializers too: an interface annotation can hide the
// concrete property's declaration, and arrays expose only an index signature.
func (b *irBuilder) reflectionContainerValue(owner *ast.Node, names []string, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	visited := map[*ast.Node]bool{}
	result := 0
	add := func(value int) {
		if value > result {
			result = value
		}
	}
	matches := func(name string) bool {
		if names == nil {
			return true
		}
		for _, selected := range names {
			if selected == name {
				return true
			}
		}
		return false
	}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n == nil || visited[n] {
			return
		}
		visited[n] = true
		switch n.Kind {
		case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindSatisfiesExpression, ast.KindNonNullExpression, ast.KindTypeAssertionExpression:
			visit(n.Expression())
		case ast.KindIdentifier:
			symbol := b.c.GetSymbolAtLocation(n)
			if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
				symbol = b.c.GetAliasedSymbol(symbol)
			}
			if symbol == nil || symbol.ValueDeclaration == nil {
				return
			}
			declaration := symbol.ValueDeclaration
			if declaration.Kind == ast.KindVariableDeclaration || declaration.Kind == ast.KindParameter {
				visit(declaration.Initializer())
				for _, source := range b.reflectionSources[declaration] {
					visit(source)
				}
			}
		case ast.KindObjectLiteralExpression:
			for _, property := range n.AsObjectLiteralExpression().Properties.Nodes {
				if property.Kind == ast.KindSpreadAssignment {
					visit(property.Expression())
					continue
				}
				name := property.Name()
				if name == nil {
					continue
				}
				computed := name.Kind == ast.KindComputedPropertyName
				if computed {
					name = name.Expression()
				}
				if name.Kind == ast.KindIdentifier && !computed || name.Kind == ast.KindStringLiteral || name.Kind == ast.KindNumericLiteral {
					if !matches(name.Text()) {
						continue
					}
				}
				add(b.reflectionBindingValue(property, seen, classify))
			}
		case ast.KindArrayLiteralExpression:
			for index, item := range reflectionLiteralArguments(n.AsArrayLiteralExpression().Elements.Nodes) {
				if item.Kind != ast.KindOmittedExpression && matches(strconv.Itoa(index)) {
					add(classify(item, seen))
				}
			}
		case ast.KindCallExpression:
			add(b.reflectionReturnedValue(n, visited, func(value *ast.Node, _ map[*ast.Node]bool) int { visit(value); return result }))
		case ast.KindConditionalExpression:
			visit(n.AsConditionalExpression().WhenTrue)
			visit(n.AsConditionalExpression().WhenFalse)
		case ast.KindBinaryExpression:
			expression := n.AsBinaryExpression()
			if expression.OperatorToken.Kind != ast.KindCommaToken && expression.OperatorToken.Kind != ast.KindEqualsToken {
				visit(expression.Left)
			}
			visit(expression.Right)
		}
	}
	visit(owner)
	return result
}

func (b *irBuilder) reflectionReturnedValue(n *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	signature := b.c.GetResolvedSignature(n)
	if signature == nil {
		return 0
	}
	if signature.Target() != nil {
		signature = signature.Target()
	}
	declaration := signature.Declaration()
	if declaration == nil || !ast.IsFunctionLike(declaration) || declaration.Body() == nil || seen[declaration] {
		return 0
	}
	seen[declaration] = true
	return b.reflectionBodyValue(declaration.Body(), seen, classify)
}

func (b *irBuilder) reflectionBodyValue(body *ast.Node, seen map[*ast.Node]bool, classify func(*ast.Node, map[*ast.Node]bool) int) int {
	if body == nil {
		return 0
	}
	if body.Kind != ast.KindBlock {
		return classify(body, seen)
	}
	result := 0
	var visit func(*ast.Node) bool
	visit = func(node *ast.Node) bool {
		if ast.IsFunctionLike(node) {
			return false
		}
		if node.Kind == ast.KindReturnStatement {
			if value := classify(node.Expression(), seen); value > result {
				result = value
			}
			return false
		}
		node.ForEachChild(visit)
		return false
	}
	visit(body)
	return result
}

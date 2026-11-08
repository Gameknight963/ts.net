package main

import "github.com/microsoft/typescript-go/internal/ast"

func assignmentPatternNode(n *ast.Node) *ast.Node {
	for n.Kind == ast.KindParenthesizedExpression {
		n = n.Expression()
	}
	return n
}
func (b *irBuilder) assignmentPattern(n *ast.Node) int {
	n = assignmentPatternNode(n)
	object := n.Kind == ast.KindObjectLiteralExpression
	id := b.node(36, n, "assignment pattern")
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	if object {
		b.nodes[id-1].constant = 1
	}
	b.nodes[id-1].fourth = b.node(20, n, "assignment pattern")
	var items []*ast.Node
	if object {
		items = n.AsObjectLiteralExpression().Properties.Nodes
	} else {
		items = n.AsArrayLiteralExpression().Elements.Nodes
	}
	last := 0
	for _, item := range items {
		child := 0
		if item.Kind == ast.KindOmittedExpression {
			child = b.lower(item)
		} else {
			child = b.node(37, item, "assignment binding")
			b.nodes[child-1].name = b.text("$assignment")
			b.nodes[child-1].typ = b.typ(b.c.GetAnyType())
			target := item
			var key, initial *ast.Node
			rest := item.Kind == ast.KindSpreadElement || item.Kind == ast.KindSpreadAssignment
			if rest {
				target = item.Expression()
				b.nodes[child-1].constant = 1
				if !object {
					b.types = append(b.types, irType{kind: 7, element: b.typ(b.c.GetAnyType())})
					b.nodes[child-1].typ = len(b.types)
				}
			} else if object {
				key = item.Name()
				if item.Kind == ast.KindShorthandPropertyAssignment {
					target = item.Name()
					initial = item.AsShorthandPropertyAssignment().ObjectAssignmentInitializer
				} else {
					target = item.Initializer()
				}
			}
			target = assignmentPatternNode(target)
			if target.Kind == ast.KindBinaryExpression && target.AsBinaryExpression().OperatorToken.Kind == ast.KindEqualsToken {
				initial = target.AsBinaryExpression().Right
				target = assignmentPatternNode(target.AsBinaryExpression().Left)
			}
			b.nodes[child-1].left = b.lower(initial)
			if target.Kind == ast.KindArrayLiteralExpression || target.Kind == ast.KindObjectLiteralExpression {
				b.nodes[child-1].right = b.assignmentPattern(target)
			} else {
				assignment := b.node(10, target, "=")
				b.nodes[assignment-1].left = b.lower(target)
				lowered := b.nodes[b.nodes[assignment-1].left-1]
				// Nullable value slots preserve undefined until a default is selected.
				if !rest && lowered.kind == 3 && lowered.symbol != 0 {
					storage := b.symbols[lowered.symbol-1].typ
					if storage != 0 && (b.types[storage-1].kind == 0 || b.types[storage-1].kind == 2) {
						b.nodes[child-1].typ = storage
					}
				}
				b.nodes[assignment-1].right = b.node(20, target, "undefined")
				b.nodes[b.nodes[assignment-1].right-1].typ = b.typ(b.c.GetUndefinedType())
				b.nodes[assignment-1].typ = b.typ(b.c.GetAnyType())
				b.nodes[child-1].fourth = assignment
			}
			if key != nil {
				if key.Kind == ast.KindComputedPropertyName {
					b.nodes[child-1].third = b.lower(key.Expression())
				} else {
					text := b.node(1, key, key.Text())
					b.nodes[text-1].typ = b.typ(b.c.GetStringType())
					b.nodes[child-1].third = text
				}
			}
		}
		if last == 0 {
			b.nodes[id-1].children = child
		} else {
			b.nodes[last-1].next = child
		}
		last = child
	}
	return id
}

func assignmentPatternSuspends(n *ast.Node) bool {
	if n == nil {
		return false
	}
	if ast.IsFunctionLike(n) {
		return assignmentPatternSuspends(n.Name())
	}
	if n.Kind == ast.KindAwaitExpression || n.Kind == ast.KindYieldExpression {
		return true
	}
	found := false
	n.ForEachChild(func(child *ast.Node) bool { found = assignmentPatternSuspends(child); return found })
	return found
}
func (b *irBuilder) assignmentPatternValue(n *ast.Node, pattern, source int) int {
	if assignmentPatternSuspends(n) {
		b.nodes[pattern-1].left = source
		return pattern
	}
	// Keep iterator cleanup in a method with an empty evaluation stack. The
	// caller may already have operands on its stack when this expression runs.
	function, value := b.newFunction(n, "")
	typ := b.nodes[pattern-1].typ
	b.nodes[function-1].typ = typ
	b.symbols[b.nodes[function-1].symbol-1].typ = typ
	b.nodes[value-1].typ = typ
	any := b.typ(b.c.GetAnyType())
	b.symbols = append(b.symbols, irSymbol{name: b.text("$destructureSource"), typ: any})
	symbol := len(b.symbols)
	parameter := b.node(13, n, "$destructureSource")
	b.nodes[parameter-1].symbol = symbol
	b.nodes[parameter-1].typ = any
	b.nodes[function-1].children = parameter
	reference := func() int {
		id := b.node(3, n, "$destructureSource")
		b.nodes[id-1].symbol = symbol
		b.nodes[id-1].typ = any
		return id
	}
	b.nodes[pattern-1].left = reference()
	returned := b.node(14, n, "return")
	b.nodes[returned-1].left = reference()
	b.nodes[pattern-1].next = returned
	body := b.node(12, n, "destructuring assignment")
	b.nodes[body-1].children = pattern
	b.nodes[function-1].left = body
	call := b.node(6, n, "destructuring assignment")
	b.nodes[call-1].left = value
	b.nodes[call-1].children = source
	b.nodes[call-1].typ = typ
	return call
}

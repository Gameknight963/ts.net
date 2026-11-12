package main

import "strconv"

const asyncBindingScope = 73

// Scope markers keep bindings in their source environment when a suspension
// moves initialization into a synthetic continuation. They execute no code.
func (a *asyncExpressionLowering) bindingScopes(id, function, scope int, seen map[int]bool) []int {
	if id == 0 {
		return nil
	}
	n := a.b.nodes[id-1]
	if n.kind == 19 || n.kind == 38 || n.kind == 25 || n.kind == 26 {
		return nil
	}
	if n.kind == 12 && n.name != 0 || n.kind == 17 || n.kind == 34 || n.kind == 68 || n.kind == 27 && n.constant == 0 {
		scope = id
	}
	var markers []int
	if (n.kind == 13 || n.kind == 37) && n.symbol != 0 && !seen[n.symbol] {
		seen[n.symbol] = true
		owner := scope
		variable := n.kind == 13 && n.fourth != 0 || n.kind == 37 && n.name != 0 && a.b.texts[n.name-1] == "var"
		if variable {
			owner = function
		}
		marker := a.node(asyncBindingScope, "async binding scope")
		a.b.nodes[marker-1].symbol = n.symbol
		a.b.nodes[marker-1].typ = a.b.symbols[n.symbol-1].typ
		if variable {
			a.b.nodes[marker-1].name = a.b.text("var")
		}
		descriptor := a.node(0, strconv.Itoa(owner))
		a.b.nodes[marker-1].left = descriptor
		markers = append(markers, marker)
	}
	for _, child := range []int{n.left, n.right, n.third, n.fourth} {
		markers = append(markers, a.bindingScopes(child, function, scope, seen)...)
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		markers = append(markers, a.bindingScopes(child, function, scope, seen)...)
	}
	return markers
}
func (a *asyncExpressionLowering) body(root, function int) int {
	markers := a.bindingScopes(root, function, root, map[int]bool{})
	result := a.statement(root, func() int { return a.returned(a.node(20, "undefined")) })
	block := a.b.nodes[result-1]
	children := a.list(block.children)
	a.b.nodes[result-1].children = a.link(append(markers, children...))
	for _, transfer := range a.transfers {
		a.b.nodes[transfer.node-1].right = a.node(0, strconv.Itoa(a.continuationRegions[transfer.symbol]))
	}
	return result
}
func (a *asyncExpressionLowering) statements(values []int, next func() int) int {
	if len(values) == 0 {
		return next()
	}
	return a.statement(values[0], func() int { return a.statements(values[1:], next) })
}
func (a *asyncExpressionLowering) statement(id int, next func() int) int {
	if id == 0 {
		return next()
	}
	n := a.b.nodes[id-1]
	if !a.hasAwait(id) {
		if n.kind == 14 || n.kind == 50 {
			return id
		}
		return a.sequence(id, next())
	}
	if n.kind == 12 {
		children := a.list(n.children)
		a.b.nodes[id-1].children = 0
		a.b.nodes[id-1].children = a.statements(children, next)
		return id
	}
	if n.kind == 27 {
		return a.switchStatement(id, n, next)
	}
	if n.kind == 17 {
		return a.forStatement(id, n, next)
	}
	if n.kind == 34 || n.kind == 68 {
		return a.forEachStatement(id, n, next)
	}
	a.discard(id)
	switch n.kind {
	case 14:
		if a.asyncGenerator {
			return a.expression(n.left, func(value int) int {
				if value == 0 {
					value = a.node(20, "undefined")
				}
				callback := a.function(true, func(parameter int) int { return a.returned(a.reference(parameter)) })
				await := a.node(70, "async generator return")
				a.b.nodes[await-1].left = value
				a.b.nodes[await-1].right = callback
				return a.returned(await)
			})
		}
		return a.expression(n.left, a.returned)
	case 51:
		return a.tryStatement(id, n, next)
	case 16, 24:
		return a.whileStatement(id, n, next)
	case 29: // labeled statement; transfers unwind synchronous finally blocks
		continuation := a.function(false, func(_ int) int { return next() })
		return a.bind(continuation, func(done int) int {
			a.rewriteBreaks(n.left, id, done)
			return a.statement(n.left, func() int { return a.resume(done) })
		})
	case 13, 18, 50: // declaration, expression statement, throw
		return a.expression(n.left, func(value int) int {
			n.left = value
			statement := a.operation(n)
			if n.kind == 50 {
				return statement
			}
			return a.sequence(statement, next())
		})
	case 36:
		for child := n.children; child != 0; child = a.b.nodes[child-1].next {
			if a.hasAwait(child) {
				return a.expression(n.left, func(value int) int { return a.pattern(n, value, next) })
			}
		}
		return a.expression(n.left, func(value int) int { n.left = value; return a.sequence(a.operation(n), next()) })
	case 15: // if: one shared fallthrough continuation, with no extra Promise jobs
		tail := next()
		rightAwaits := a.hasAwait(n.right)
		if rightAwaits != a.hasAwait(n.third) {
			if plain := a.copyBranchTail(tail); plain != 0 {
				awaiting, ordinary := n.right, n.third
				if !rightAwaits {
					awaiting, ordinary = ordinary, awaiting
				}
				continuation := a.function(false, func(_ int) int { return tail })
				bound := a.bind(continuation, func(symbol int) int {
					return a.statement(awaiting, func() int {
						call := a.node(6, "async statement join")
						a.b.nodes[call-1].left = a.reference(symbol)
						return a.returned(call)
					})
				})
				// Captured join bindings belong only to the suspending branch.
				a.b.nodes[bound-1].name = a.b.text("scope")
				other := a.statement(ordinary, func() int { return plain })
				yes, no := bound, other
				if !rightAwaits {
					yes, no = other, bound
				}
				return a.expression(n.left, func(condition int) int { return a.branch(condition, yes, no) })
			}
		}
		continuation := a.function(false, func(_ int) int { return tail })
		return a.bind(continuation, func(symbol int) int {
			join := func() int {
				call := a.node(6, "async statement join")
				a.b.nodes[call-1].left = a.reference(symbol)
				return a.returned(call)
			}
			return a.expression(n.left, func(condition int) int {
				return a.branch(condition, a.statement(n.right, join), a.statement(n.third, join))
			})
		})
	}
	a.b.unsupported(a.source)
	return 0
}

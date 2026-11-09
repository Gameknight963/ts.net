package main

import "strconv"

func (a *asyncExpressionLowering) declaration(symbol, value int) int {
	a.recordContinuation(symbol, value)
	id := a.node(13, "async loop binding")
	a.b.nodes[id-1].symbol = symbol
	a.b.nodes[id-1].typ = a.b.symbols[symbol-1].typ
	a.b.nodes[id-1].left = value
	return id
}

func (a *asyncExpressionLowering) loopBindings(id int, bindings map[int]int, order *[]int) {
	if id == 0 {
		return
	}
	n := a.b.nodes[id-1]
	if n.kind == 19 || n.kind == 38 || n.kind == 25 || n.kind == 26 {
		return
	}
	if n.kind == 13 && n.fourth == 0 || n.kind == 37 && (n.name == 0 || a.b.texts[n.name-1] != "var") {
		if n.symbol != 0 && bindings[n.symbol] == 0 {
			bindings[n.symbol] = a.symbol(a.b.symbols[n.symbol-1].typ)
			*order = append(*order, n.symbol)
		}
	}
	// Initializer expressions may contain declarations unrelated to the loop.
	if n.kind != 12 && n.kind != 36 && n.kind != 37 {
		return
	}
	if n.kind == 37 {
		a.loopBindings(n.right, bindings, order)
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		a.loopBindings(child, bindings, order)
	}
}

func (a *asyncExpressionLowering) renameLoopReferences(id int, bindings map[int]int, seen map[int]bool) {
	if id == 0 || seen[id] {
		return
	}
	seen[id] = true
	n := a.b.nodes[id-1]
	if n.kind == 25 || n.kind == 26 {
		return
	}
	if n.kind == 3 && bindings[n.symbol] != 0 {
		a.b.nodes[id-1].symbol = bindings[n.symbol]
	}
	for _, child := range []int{n.left, n.right, n.third, n.fourth} {
		a.renameLoopReferences(child, bindings, seen)
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		a.renameLoopReferences(child, bindings, seen)
	}
}

func (a *asyncExpressionLowering) loopScopeMarker(symbol, scope int) int {
	marker := a.node(asyncBindingScope, "loop binding scope")
	a.b.nodes[marker-1].symbol = symbol
	a.b.nodes[marker-1].typ = a.b.symbols[symbol-1].typ
	a.b.nodes[marker-1].left = a.node(0, strconv.Itoa(scope))
	return marker
}

func (a *asyncExpressionLowering) callLoop(step int, increment bool, symbols []int) int {
	flag := a.node(2, strconv.FormatBool(increment))
	a.b.nodes[flag-1].typ = a.b.typ(a.b.c.GetBooleanType())
	arguments := []int{flag}
	for _, symbol := range symbols {
		arguments = append(arguments, a.reference(symbol))
	}
	call := a.node(6, "async loop step")
	a.b.nodes[call-1].left = a.reference(step)
	a.b.nodes[call-1].children = a.link(arguments)
	return a.returned(call)
}

func (a *asyncExpressionLowering) forStatement(id int, n irNode, next func() int) int {
	bindings := map[int]int{}
	var originals []int
	a.loopBindings(n.left, bindings, &originals)
	if !a.capturedForBinding(id, bindings, false, map[[2]int]bool{}) {
		return a.sharedForStatement(id, n, next)
	}
	var copies []int
	for _, symbol := range originals {
		copies = append(copies, bindings[symbol])
	}
	for _, child := range []int{n.right, n.third, n.fourth} {
		a.renameLoopReferences(child, bindings, map[int]bool{})
	}
	continuation := a.function(false, func(_ int) int { return next() })
	body := a.bind(continuation, func(done int) int {
		step := a.symbol(a.any)
		advance := a.symbol(a.any)
		check := a.symbol(a.any)
		a.rewriteBreaks(n.fourth, id, done)
		a.rewriteJumps(n.fourth, id, advance, 26)
		iteration := a.node(12, "for iteration scope")
		a.b.nodes[iteration-1].name = a.b.text("scope")
		function := a.functionParameters(len(copies)+1, func(parameters []int) int {
			var statements []int
			for i, symbol := range copies {
				statements = append(statements, a.loopScopeMarker(symbol, iteration), a.declaration(symbol, a.reference(parameters[i+1])))
			}
			// Read the previous iteration's values after any finally blocks run.
			advanceFunction := a.function(false, func(_ int) int { return a.callLoop(step, true, copies) })
			statements = append(statements, a.declaration(advance, advanceFunction))
			checkFunction := a.function(false, func(_ int) int {
				execute := a.statement(n.fourth, func() int { return a.resume(advance) })
				if n.right == 0 {
					return execute
				}
				return a.expression(n.right, func(condition int) int { return a.branch(condition, execute, a.resume(done)) })
			})
			statements = append(statements, a.declaration(check, checkFunction))
			update := a.resume(check)
			if n.third != 0 {
				update = a.expression(n.third, func(value int) int {
					statement := a.node(18, "for update")
					a.b.nodes[statement-1].left = value
					return a.sequence(statement, a.resume(check))
				})
			}
			statements = append(statements, a.branch(a.reference(parameters[0]), update, a.resume(check)))
			a.b.nodes[iteration-1].children = a.link(statements)
			return iteration
		})
		return a.statement(n.left, func() int { return a.sequence(a.declaration(step, function), a.callLoop(step, false, originals)) })
	})
	a.b.nodes[id-1] = irNode{kind: 12, token: n.token, name: a.b.text("scope"), children: body}
	return id
}

// Per-iteration let bindings need separate storage only when a nested function
// or class can retain them. Be conservative about every reference in those trees.
func (a *asyncExpressionLowering) capturedForBinding(id int, bindings map[int]int, nested bool, seen map[[2]int]bool) bool {
	if id == 0 {
		return false
	}
	n := a.b.nodes[id-1]
	// Break and continue contain back references to their target statements.
	if n.kind == 25 || n.kind == 26 {
		return false
	}
	nested = nested || n.kind == 19 || n.kind == 38 || n.kind == 55
	key := [2]int{id, 0}
	if nested {
		key[1] = 1
	}
	if seen[key] {
		return false
	}
	seen[key] = true
	if nested && n.kind == 3 && bindings[n.symbol] != 0 {
		return true
	}
	for _, child := range []int{n.left, n.right, n.third, n.fourth} {
		if a.capturedForBinding(child, bindings, nested, seen) {
			return true
		}
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		if a.capturedForBinding(child, bindings, nested, seen) {
			return true
		}
	}
	return false
}

func (a *asyncExpressionLowering) sharedForStatement(id int, n irNode, next func() int) int {
	continuation := a.function(false, func(_ int) int { return next() })
	body := a.bind(continuation, func(done int) int {
		step := a.symbol(a.any)
		advance := a.symbol(a.any)
		a.rewriteBreaks(n.fourth, id, done)
		a.rewriteJumps(n.fourth, id, advance, 26)
		advanceFunction := a.function(false, func(_ int) int {
			if n.third == 0 {
				return a.resume(step)
			}
			return a.expression(n.third, func(value int) int {
				statement := a.node(18, "for update")
				a.b.nodes[statement-1].left = value
				return a.sequence(statement, a.resume(step))
			})
		})
		stepFunction := a.function(false, func(_ int) int {
			execute := a.statement(n.fourth, func() int { return a.resume(advance) })
			if n.right == 0 {
				return execute
			}
			return a.expression(n.right, func(condition int) int { return a.branch(condition, execute, a.resume(done)) })
		})
		return a.statement(n.left, func() int {
			return a.sequence(a.declaration(step, stepFunction), a.sequence(a.declaration(advance, advanceFunction), a.resume(step)))
		})
	})
	a.b.nodes[id-1] = irNode{kind: 12, token: n.token, name: a.b.text("scope"), children: body}
	return id
}

func (a *asyncExpressionLowering) renameLoopDeclarations(id int, bindings map[int]int) {
	if id == 0 {
		return
	}
	n := a.b.nodes[id-1]
	if n.kind == 13 || n.kind == 37 {
		if bindings[n.symbol] != 0 {
			a.b.nodes[id-1].symbol = bindings[n.symbol]
		}
	}
	if n.kind == 37 {
		a.renameLoopDeclarations(n.right, bindings)
	}
	if n.kind == 12 || n.kind == 36 {
		for child := n.children; child != 0; child = a.b.nodes[child-1].next {
			a.renameLoopDeclarations(child, bindings)
		}
	}
}

func (a *asyncExpressionLowering) forEachStatement(id int, n irNode, next func() int) int {
	bindings := map[int]int{}
	var originals []int
	a.loopBindings(n.left, bindings, &originals)
	for _, child := range []int{n.left, n.fourth} {
		a.renameLoopReferences(child, bindings, map[int]bool{})
	}
	a.renameLoopDeclarations(n.left, bindings)
	continuation := a.function(false, func(_ int) int { return next() })
	body := a.bind(continuation, func(done int) int {
		return a.expression(n.right, func(source int) int {
			start := a.node(78, "async iteration start")
			a.b.nodes[start-1].left = source
			if n.kind == 68 {
				a.b.nodes[start-1].constant = 1
			}
			return a.bind(start, func(cursor int) int {
				build := func() int {
					step := a.symbol(a.any)
					body := a.symbol(a.any)
					a.rewriteBreaks(n.fourth, id, done)
					a.rewriteJumps(n.fourth, id, step, 26)
					iteration := a.node(12, "for-each iteration scope")
					a.b.nodes[iteration-1].name = a.b.text("scope")
					bodyFunction := a.function(false, func(_ int) int {
						var statements []int
						for _, symbol := range originals {
							statements = append(statements, a.loopScopeMarker(bindings[symbol], iteration))
						}
						value := a.node(80, "async iteration value")
						a.b.nodes[value-1].left = a.reference(cursor)
						binding := n.left
						if a.b.nodes[binding-1].kind == 12 {
							binding = a.b.nodes[binding-1].children
						}
						target := a.b.nodes[binding-1]
						if target.kind == 13 || target.kind == 36 {
							a.b.nodes[binding-1].left = value
						} else {
							assignment := a.node(10, "=")
							a.b.nodes[assignment-1].left = binding
							a.b.nodes[assignment-1].right = value
							a.b.nodes[assignment-1].typ = target.typ
							binding = a.node(18, "async iteration assignment")
							a.b.nodes[binding-1].left = assignment
						}
						statements = append(statements, a.statement(binding, func() int {
							return a.statement(n.fourth, func() int { return a.resume(step) })
						}))
						a.b.nodes[iteration-1].children = a.link(statements)
						return iteration
					})
					stepFunction := a.function(false, func(_ int) int {
						if n.third != 0 {
							request := a.node(83, "for await next")
							a.b.nodes[request-1].left = a.reference(cursor)
							await := a.node(rawAsyncAwait, "for await next")
							a.b.nodes[await-1].left = request
							return a.expression(await, func(result int) int {
								condition := a.node(84, "for await result")
								a.b.nodes[condition-1].typ = a.b.typ(a.b.c.GetBooleanType())
								a.b.nodes[condition-1].left = a.reference(cursor)
								a.b.nodes[condition-1].right = result
								return a.branch(condition, a.resume(body), a.tryEnd(id))
							})
						}
						condition := a.node(79, "async iteration next")
						a.b.nodes[condition-1].left = a.reference(cursor)
						a.b.nodes[condition-1].typ = a.b.typ(a.b.c.GetBooleanType())
						if n.kind == 34 {
							return a.branch(condition, a.resume(body), a.tryEnd(id))
						}
						return a.branch(condition, a.resume(body), a.resume(done))
					})
					return a.sequence(a.declaration(step, stepFunction), a.sequence(a.declaration(body, bodyFunction), a.resume(step)))
				}
				if n.kind == 34 {
					return a.forAwaitRegion(id, cursor, done, n.third != 0, build)
				}
				return build()
			})
		})
	})
	a.b.nodes[id-1] = irNode{kind: 12, token: n.token, name: a.b.text("scope"), children: body}
	return id
}

func (a *asyncExpressionLowering) whileStatement(id int, n irNode, next func() int) int {
	continuation := a.function(false, func(_ int) int { return next() })
	return a.bind(continuation, func(done int) int {
		step := a.symbol(a.any)
		body := a.symbol(a.any)
		a.rewriteBreaks(n.right, id, done)
		a.rewriteJumps(n.right, id, step, 26)
		bodyFunction := a.function(false, func(_ int) int {
			return a.statement(n.right, func() int { return a.resume(step) })
		})
		stepFunction := a.function(false, func(_ int) int {
			return a.expression(n.left, func(condition int) int {
				return a.branch(condition, a.resume(body), a.resume(done))
			})
		})
		start := step
		if n.kind == 24 {
			start = body
		}
		return a.sequence(a.declaration(step, stepFunction),
			a.sequence(a.declaration(body, bodyFunction), a.resume(start)))
	})
}

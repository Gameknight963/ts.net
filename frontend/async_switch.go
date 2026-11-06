package main

func (a *asyncExpressionLowering) resume(symbol int) int {
	return a.returned(a.transfer(symbol))
}
func (a *asyncExpressionLowering) transfer(symbol int) int {
	resume := a.node(77, "async synchronous transfer")
	a.b.nodes[resume-1].left = a.reference(symbol)
	a.transfers = append(a.transfers, asyncTransfer{resume, symbol})
	return resume
}

// Switch breaks may cross a synchronous try/finally. Return a transfer record
// rather than calling the continuation before that finally has executed.
func (a *asyncExpressionLowering) rewriteBreaks(id, target, continuation int) {
	a.rewriteJumps(id, target, continuation, 25)
}

func (a *asyncExpressionLowering) rewriteJumps(id, target, continuation, kind int) {
	if id == 0 {
		return
	}
	n := a.b.nodes[id-1]
	if n.kind == 19 || n.kind == 38 {
		return
	}
	if n.kind == 25 || n.kind == 26 {
		if n.kind == kind && n.left == target {
			resume := a.transfer(continuation)
			a.b.nodes[id-1] = irNode{kind: 14, token: n.token, left: resume, next: n.next}
		}
		return
	}
	for _, child := range []int{n.left, n.right, n.third, n.fourth} {
		a.rewriteJumps(child, target, continuation, kind)
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		a.rewriteJumps(child, target, continuation, kind)
	}
}

func (a *asyncExpressionLowering) switchStatement(id int, n irNode, next func() int) int {
	clauses := a.list(n.children)
	continuation := a.function(false, func(_ int) int { return next() })
	body := a.bind(continuation, func(done int) int {
		bodies := make([]int, len(clauses))
		for i := range bodies {
			bodies[i] = a.symbol(a.any)
		}
		declarations := make([]int, len(clauses))
		fallback := done
		for i, clause := range clauses {
			item := a.b.nodes[clause-1]
			if item.left == 0 {
				fallback = bodies[i]
			}
			following := done
			if i+1 < len(bodies) {
				following = bodies[i+1]
			}
			statements := a.list(item.children)
			for _, statement := range statements {
				a.rewriteBreaks(statement, id, done)
			}
			function := a.function(false, func(_ int) int {
				return a.statements(statements, func() int { return a.resume(following) })
			})
			declaration := a.node(13, "async switch case")
			a.recordContinuation(bodies[i], function)
			a.b.nodes[declaration-1].symbol = bodies[i]
			a.b.nodes[declaration-1].left = function
			declarations[i] = declaration
		}
		selectCase := a.expression(n.left, func(value int) int {
			return a.bind(value, func(selected int) int {
				var search func(int) int
				search = func(index int) int {
					if index == len(clauses) {
						return a.resume(fallback)
					}
					item := a.b.nodes[clauses[index]-1]
					if item.left == 0 {
						return search(index + 1)
					}
					return a.expression(item.left, func(value int) int {
						comparison := a.node(7, "===")
						a.b.nodes[comparison-1].typ = a.b.typ(a.b.c.GetBooleanType())
						a.b.nodes[comparison-1].left = a.reference(selected)
						a.b.nodes[comparison-1].right = value
						return a.branch(comparison, a.resume(bodies[index]), search(index+1))
					})
				}
				return search(0)
			})
		})
		for _, clause := range clauses {
			a.discard(clause)
		}
		return a.statements(declarations, func() int { return selectCase })
	})
	// Retain the original scope ID for bindings captured across case functions.
	block := irNode{kind: 12, token: n.token, children: body}
	if n.constant == 0 {
		block.name = a.b.text("scope")
	}
	a.b.nodes[id-1] = block
	return id
}

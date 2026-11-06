package main

import "strconv"

func (a *asyncExpressionLowering) forAwaitRegion(id, cursor, done int, awaiting bool, build func() int) int {
	after := a.function(false, func(_ int) int { return a.resume(done) })
	previous := a.region
	a.region = id
	body := a.function(false, func(_ int) int { return build() })
	finalizer := a.function(false, func(_ int) int {
		if !awaiting {
			close := a.node(89, "iterator close")
			a.b.nodes[close-1].left = a.reference(cursor)
			statement := a.node(18, "iterator close")
			a.b.nodes[statement-1].left = close
			condition := a.node(85, "iterator needs close")
			a.b.nodes[condition-1].typ = a.b.typ(a.b.c.GetBooleanType())
			a.b.nodes[condition-1].left = a.reference(cursor)
			return a.branch(condition, a.sequence(statement, a.tryEnd(id)), a.tryEnd(id))
		}
		condition := a.node(85, "for await needs close")
		a.b.nodes[condition-1].typ = a.b.typ(a.b.c.GetBooleanType())
		a.b.nodes[condition-1].left = a.reference(cursor)
		close := a.node(86, "for await close")
		a.b.nodes[close-1].left = a.reference(cursor)
		await := a.node(rawAsyncAwait, "for await close")
		a.b.nodes[await-1].left = close
		closing := a.expression(await, func(result int) int {
			check := a.node(87, "iterator close result")
			a.b.nodes[check-1].left = result
			statement := a.node(18, "iterator close result")
			a.b.nodes[statement-1].left = check
			return a.sequence(statement, a.tryEnd(id))
		})
		return a.branch(condition, closing, a.tryEnd(id))
	})
	a.region = previous
	enter := a.node(88, "for await protected iteration")
	a.b.nodes[enter-1].name = a.b.text(strconv.Itoa(id))
	a.b.nodes[enter-1].left = body
	a.b.nodes[enter-1].right = finalizer
	a.b.nodes[enter-1].third = after
	return a.returned(enter)
}

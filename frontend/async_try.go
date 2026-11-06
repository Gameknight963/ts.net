package main

import "strconv"

func (a *asyncExpressionLowering) tryEnd(region int) int {
	end := a.node(82, "async try completion")
	a.b.nodes[end-1].name = a.b.text(strconv.Itoa(region))
	return a.returned(end)
}

func (a *asyncExpressionLowering) tryStatement(id int, n irNode, next func() int) int {
	after := a.function(false, func(_ int) int { return next() })
	previous := a.region
	a.region = id
	body := a.function(false, func(_ int) int { return a.statement(n.left, func() int { return a.tryEnd(id) }) })
	catch := a.node(20, "undefined")
	if n.right != 0 {
		catch = a.function(true, func(parameter int) int {
			binding := a.b.nodes[n.right-1].children
			if binding != 0 {
				value := a.b.nodes[binding-1].left
				if value != 0 && a.b.nodes[value-1].kind == 52 {
					a.discard(value)
					a.b.nodes[binding-1].left = a.reference(parameter)
				}
			}
			return a.statement(n.right, func() int { return a.tryEnd(id) })
		})
	}
	finalizer := a.node(20, "undefined")
	if n.third != 0 {
		finalizer = a.function(false, func(_ int) int { return a.statement(n.third, func() int { return a.tryEnd(id) }) })
	}
	a.region = previous
	enter := a.node(81, "async try")
	a.b.nodes[enter-1].name = a.b.text(strconv.Itoa(id))
	a.b.nodes[enter-1].left = body
	a.b.nodes[enter-1].right = catch
	a.b.nodes[enter-1].third = finalizer
	a.b.nodes[enter-1].fourth = after
	return a.returned(enter)
}

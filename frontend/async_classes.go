package main

import "strconv"

func (a *asyncExpressionLowering) classNumber(value int) int {
	id := a.node(0, strconv.Itoa(value))
	a.b.nodes[id-1].typ = a.b.typ(a.b.c.GetNumberType())
	return id
}
func (a *asyncExpressionLowering) classText(value int) int {
	text := ""
	if value != 0 {
		text = a.b.texts[value-1]
	}
	id := a.node(1, text)
	a.b.nodes[id-1].typ = a.b.typ(a.b.c.GetStringType())
	return id
}
func (a *asyncExpressionLowering) classExpression(n irNode, next asyncExpressionContinuation) int {
	// Create the class before adding its members, keeping keys in source order.
	members := a.list(n.children)
	n.children = 0
	return a.expression(n.third, func(parent int) int {
		n.third = parent
		return a.bind(a.operation(n), func(class int) int {
			return a.classMembers(members, class, func() int { return next(a.reference(class)) })
		})
	})
}
func (a *asyncExpressionLowering) classMembers(members []int, class int, next func() int) int {
	if len(members) == 0 {
		return next()
	}
	id := members[0]
	n := a.b.nodes[id-1]
	a.discard(id)
	rest := func() int { return a.classMembers(members[1:], class, next) }
	store := func(value int) int {
		n.kind = 13
		n.typ = a.b.symbols[n.symbol-1].typ
		n.constant = 0
		n.left = value
		n.right = 0
		n.third = 0
		n.fourth = 0
		n.children = 0
		n.next = 0
		return a.sequence(a.operation(n), rest())
	}
	if n.kind == 18 {
		return a.expression(n.left, func(value int) int { n.left = value; return a.sequence(a.operation(n), rest()) })
	}
	if n.kind == 13 {
		return a.expression(n.left, func(key int) int { return store(a.b.builtinCall(a.source, "__classKey", key)) })
	}
	if n.fourth != 0 {
		marker := a.b.nodes[n.fourth-1]
		kind, err := strconv.Atoi(a.b.texts[a.b.tokens[marker.token-1].text-1])
		if err != nil {
			a.b.unsupported(a.source)
		}
		function := n.left
		if function == 0 {
			function = a.node(20, "undefined")
		}
		return store(a.b.builtinCall(a.source, "__classPrivate", a.reference(class), a.classText(n.name), a.classNumber(kind), function, n.right))
	}
	return a.expression(n.right, func(key int) int {
		if key == 0 {
			key = a.classText(n.name)
		}
		static := a.classNumber(n.constant)
		if n.symbol != 0 && n.left == 0 {
			return store(a.b.builtinCall(a.source, "__classAccessor", a.reference(class), key, static))
		}
		mode := 0
		if n.third != 0 {
			mode = 2
			if a.b.nodes[n.third-1].constant != 0 {
				mode = 1
			}
		}
		call := a.b.builtinCall(a.source, "__classMethod", a.reference(class), key, n.left, static, a.classNumber(mode))
		statement := a.node(18, "class member")
		a.b.nodes[statement-1].left = call
		return a.sequence(statement, rest())
	})
}

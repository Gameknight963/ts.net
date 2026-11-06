package main

// Keep the iterator position and excluded object keys across suspensions. Reads
// happen one at a time, so a default may change values read by later bindings.
func (a *asyncExpressionLowering) pattern(n irNode, source int, next func() int) int {
	state := a.node(74, "destructuring state")
	a.b.nodes[state-1].left = source
	a.b.nodes[state-1].constant = n.constant
	return a.bind(state, func(cursor int) int {
		var walk func(int) int
		walk = func(child int) int {
			if child == 0 {
				return next()
			}
			binding := a.b.nodes[child-1]
			a.discard(child)
			finish := func(value int) int {
				if binding.kind == 20 {
					statement := a.node(18, "destructuring hole")
					a.b.nodes[statement-1].left = value
					return a.sequence(statement, walk(binding.next))
				}
				return a.binding(binding, value, func() int { return walk(binding.next) })
			}
			read := func(key int) int {
				value := a.node(75, "destructuring read")
				a.b.nodes[value-1].left = a.reference(cursor)
				a.b.nodes[value-1].right = key
				if binding.kind == 37 && binding.constant != 0 {
					a.b.nodes[value-1].kind = 76
					a.b.nodes[value-1].constant = n.constant
					a.b.nodes[value-1].typ = binding.typ
				}
				return finish(value)
			}
			if n.constant != 0 && binding.constant == 0 {
				return a.expression(binding.third, read)
			}
			return read(a.node(20, "undefined"))
		}
		return walk(n.children)
	})
}

func (a *asyncExpressionLowering) binding(n irNode, value int, next func() int) int {
	finish := func(value int) int {
		if n.right != 0 {
			pattern := a.b.nodes[n.right-1]
			a.discard(n.right)
			return a.pattern(pattern, value, next)
		}
		declaration := a.node(13, "destructured binding")
		a.b.nodes[declaration-1].symbol = n.symbol
		a.b.nodes[declaration-1].typ = n.typ
		a.b.nodes[declaration-1].left = value
		if n.name != 0 && a.b.texts[n.name-1] == "var" {
			a.b.nodes[declaration-1].fourth = a.node(1, "var")
		}
		return a.sequence(declaration, next())
	}
	if n.left == 0 {
		return finish(value)
	}
	return a.joined(finish, func(join func(int) int) int {
		return a.bind(value, func(saved int) int {
			condition := a.node(7, "===")
			a.b.nodes[condition-1].typ = a.b.typ(a.b.c.GetBooleanType())
			a.b.nodes[condition-1].left = a.reference(saved)
			a.b.nodes[condition-1].right = a.node(20, "undefined")
			return a.branch(condition, a.expression(n.left, join), join(a.reference(saved)))
		})
	})
}

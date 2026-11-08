package main

import "strconv"

// Keep the iterator position and excluded object keys across suspensions. Reads
// happen one at a time, so a default may change values read by later bindings.
func (a *asyncExpressionLowering) pattern(n irNode, source int, next func() int) int {
	state := a.node(74, "destructuring state")
	a.b.nodes[state-1].left = source
	a.b.nodes[state-1].constant = n.constant
	return a.bind(state, func(cursor int) int {
		finish := next
		var walk func(int) int
		walk = func(child int) int {
			if child == 0 {
				return finish()
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
			readValue := func(key int) int {
				value := a.node(75, "destructuring read")
				a.b.nodes[value-1].left = a.reference(cursor)
				a.b.nodes[value-1].right = key
				if binding.kind == 20 {
					a.b.nodes[value-1].constant = 1
				}
				if binding.kind == 37 && binding.constant != 0 {
					a.b.nodes[value-1].kind = 76
					a.b.nodes[value-1].constant = n.constant
					a.b.nodes[value-1].typ = binding.typ
				}
				return finish(value)
			}
			read := func(key int) int {
				if binding.name == 0 || a.b.texts[binding.name-1] != "$assignment" || binding.fourth == 0 {
					return readValue(key)
				}
				assignment := a.b.nodes[binding.fourth-1]
				return a.target(assignment.left, func(reference func() int) int {
					assignment.left = reference()
					binding.fourth = a.operation(assignment)
					return readValue(key)
				})
			}
			if n.constant != 0 && binding.constant == 0 {
				if binding.name != 0 && a.b.texts[binding.name-1] == "$assignment" {
					return a.expression(binding.third, func(key int) int {
						return a.bind(a.b.builtinCall(a.source, "__classKey", key), func(saved int) int { return read(a.reference(saved)) })
					})
				}
				return a.expression(binding.third, read)
			}
			return read(a.node(20, "undefined"))
		}
		if n.constant != 0 {
			return walk(n.children)
		}
		region := a.node(20, "destructuring protected region")
		after := a.function(false, func(_ int) int { return next() })
		previous := a.region
		a.region = region
		finish = func() int { return a.tryEnd(region) }
		body := a.function(false, func(_ int) int { return walk(n.children) })
		finalizer := a.function(false, func(_ int) int {
			close := a.node(90, "close destructuring")
			a.b.nodes[close-1].left = a.reference(cursor)
			statement := a.node(18, "close destructuring")
			a.b.nodes[statement-1].left = close
			return a.sequence(statement, a.tryEnd(region))
		})
		a.region = previous
		enter := a.node(88, "destructuring protected region")
		a.b.nodes[enter-1].name = a.b.text(strconv.Itoa(region))
		a.b.nodes[enter-1].left = body
		a.b.nodes[enter-1].right = finalizer
		a.b.nodes[enter-1].third = after
		return a.returned(enter)
	})
}

func (a *asyncExpressionLowering) binding(n irNode, value int, next func() int) int {
	finish := func(value int) int {
		if n.right != 0 {
			pattern := a.b.nodes[n.right-1]
			a.discard(n.right)
			return a.pattern(pattern, value, next)
		}
		if n.name != 0 && a.b.texts[n.name-1] == "$assignment" {
			assignment := a.b.nodes[n.fourth-1]
			assignment.right = value
			statement := a.node(18, "destructured assignment")
			a.b.nodes[statement-1].left = a.operation(assignment)
			return a.sequence(statement, next())
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

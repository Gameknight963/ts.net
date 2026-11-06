package main

import (
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
)

const rawAsyncAwait = -1

func expressionHasAwait(n *ast.Node) bool {
	if n == nil || ast.IsFunctionLike(n) {
		return false
	}
	if n.Kind == ast.KindAwaitExpression {
		return true
	}
	found := false
	n.ForEachChild(func(child *ast.Node) bool {
		if expressionHasAwait(child) {
			found = true
		}
		return found
	})
	return found
}
func enclosingAsyncFunction(n *ast.Node) *ast.Node {
	for parent := n.Parent; parent != nil; parent = parent.Parent {
		if ast.IsFunctionLike(parent) {
			return parent
		}
	}
	return nil
}

type asyncExpressionLowering struct {
	b      *irBuilder
	source *ast.Node
	any    int
}
type asyncExpressionContinuation func(int) int

func (a *asyncExpressionLowering) node(kind int, text string) int {
	id := a.b.node(kind, a.source, text)
	a.b.nodes[id-1].typ = a.any
	return id
}
func (a *asyncExpressionLowering) symbol(typ int) int {
	name := a.b.text("$await" + strconv.Itoa(len(a.b.symbols)+1))
	a.b.symbols = append(a.b.symbols, irSymbol{name: name, typ: typ})
	return len(a.b.symbols)
}
func (a *asyncExpressionLowering) reference(symbol int) int {
	id := a.node(3, "await value")
	a.b.nodes[id-1].symbol = symbol
	a.b.nodes[id-1].name = a.b.symbols[symbol-1].name
	a.b.nodes[id-1].typ = a.b.symbols[symbol-1].typ
	return id
}
func (a *asyncExpressionLowering) returned(value int) int {
	id := a.node(14, "return")
	a.b.nodes[id-1].left = value
	return id
}
func (a *asyncExpressionLowering) sequence(first, second int) int {
	id := a.node(12, "await sequence")
	a.b.nodes[id-1].children = first
	a.b.nodes[first-1].next = second
	return id
}
func (a *asyncExpressionLowering) bind(value int, next func(int) int) int {
	typ := a.b.nodes[value-1].typ
	if typ == 0 || a.b.types[typ-1].kind == 3 {
		typ = a.any
	}
	symbol := a.symbol(typ)
	binding := a.node(13, "await temporary")
	a.b.nodes[binding-1].typ = typ
	a.b.nodes[binding-1].symbol = symbol
	a.b.nodes[binding-1].name = a.b.text("let")
	a.b.nodes[binding-1].left = value
	return a.sequence(binding, next(symbol))
}
func (a *asyncExpressionLowering) function(parameter bool, body func(int) int) int {
	function := a.node(19, "await continuation")
	symbol := a.symbol(a.any)
	a.b.symbols[symbol-1].function = function
	a.b.nodes[function-1].symbol = symbol
	a.b.nodes[function-1].name = a.b.text("")
	a.b.nodes[function-1].next = a.b.functions
	a.b.functions = function
	argument := 0
	if parameter {
		argument = a.symbol(a.any)
		p := a.node(13, "await argument")
		a.b.nodes[p-1].symbol = argument
		a.b.nodes[function-1].children = p
	}
	a.b.nodes[function-1].left = body(argument)
	value := a.node(38, "await continuation")
	a.b.nodes[value-1].symbol = symbol
	a.b.nodes[value-1].left = function
	return value
}
func (a *asyncExpressionLowering) hasAwait(id int) bool {
	if id == 0 {
		return false
	}
	n := a.b.nodes[id-1]
	if n.kind == rawAsyncAwait {
		return true
	}
	if n.kind == 38 || n.kind == 19 {
		return false
	}
	for _, child := range []int{n.left, n.right, n.third, n.fourth} {
		if a.hasAwait(child) {
			return true
		}
	}
	for child := n.children; child != 0; child = a.b.nodes[child-1].next {
		if a.hasAwait(child) {
			return true
		}
	}
	return false
}
func (a *asyncExpressionLowering) discard(id int) {
	old := a.b.nodes[id-1]
	a.b.nodes[id-1] = irNode{kind: 20, token: old.token, typ: a.any}
}
func (a *asyncExpressionLowering) operation(n irNode) int {
	n.next = 0
	a.b.nodes = append(a.b.nodes, n)
	return len(a.b.nodes)
}
func (a *asyncExpressionLowering) list(head int) []int {
	var result []int
	for head != 0 {
		next := a.b.nodes[head-1].next
		a.b.nodes[head-1].next = 0
		result = append(result, head)
		head = next
	}
	return result
}
func (a *asyncExpressionLowering) link(values []int) int {
	for i, value := range values {
		a.b.nodes[value-1].next = 0
		if i+1 < len(values) {
			a.b.nodes[value-1].next = values[i+1]
		}
	}
	if len(values) == 0 {
		return 0
	}
	return values[0]
}
func (a *asyncExpressionLowering) branch(condition, yes, no int) int {
	id := a.node(15, "await branch")
	a.b.nodes[id-1].left = condition
	a.b.nodes[id-1].right = yes
	a.b.nodes[id-1].third = no
	return id
}

// Share a continuation instead of duplicating source expressions or function
// declarations across branches. This is a synchronous call, not a Promise.then.
func (a *asyncExpressionLowering) joined(next asyncExpressionContinuation, body func(func(int) int) int) int {
	function := a.function(true, func(parameter int) int { return next(a.reference(parameter)) })
	return a.bind(function, func(symbol int) int {
		return body(func(value int) int {
			call := a.node(6, "await join")
			a.b.nodes[call-1].left = a.reference(symbol)
			a.b.nodes[call-1].children = value
			return a.returned(call)
		})
	})
}
func (a *asyncExpressionLowering) call(n irNode, next asyncExpressionContinuation) int {
	callee := a.b.nodes[n.left-1]
	arguments := a.list(n.children)
	finish := func(target, receiver int) int {
		return a.operands(arguments, false, func(values []int) int {
			n.kind = 71
			n.left = a.reference(target)
			n.right = 0
			if receiver != 0 {
				n.right = a.reference(receiver)
			}
			n.children = a.link(values)
			return next(a.operation(n))
		})
	}
	if callee.kind == 5 || callee.kind == 33 {
		return a.expression(callee.left, func(value int) int {
			return a.bind(value, func(receiver int) int {
				a.discard(n.left)
				get := func(key int) int {
					callee.left = a.reference(receiver)
					if callee.kind == 33 {
						callee.right = key
					}
					return a.bind(a.operation(callee), func(target int) int { return finish(target, receiver) })
				}
				if callee.kind == 33 {
					return a.expression(callee.right, get)
				}
				return get(0)
			})
		})
	}
	return a.expression(n.left, func(value int) int { return a.bind(value, func(target int) int { return finish(target, 0) }) })
}
func (a *asyncExpressionLowering) expression(id int, next asyncExpressionContinuation) int {
	if !a.hasAwait(id) {
		return next(id)
	}
	n := a.b.nodes[id-1]
	a.discard(id)
	switch n.kind {
	case rawAsyncAwait:
		return a.expression(n.left, func(value int) int {
			callback := a.function(true, func(parameter int) int { return next(a.reference(parameter)) })
			await := a.node(70, "await")
			a.b.nodes[await-1].left = value
			a.b.nodes[await-1].right = callback
			return a.returned(await)
		})
	case 8, 9, 23: // unary, assertion, typeof
		return a.expression(n.left, func(value int) int { n.left = value; return next(a.operation(n)) })
	case 31: // comma
		return a.expression(n.left, func(value int) int {
			statement := a.node(18, "await prefix")
			a.b.nodes[statement-1].left = value
			return a.sequence(statement, a.expression(n.right, next))
		})
	case 22: // conditional
		return a.joined(next, func(join func(int) int) int {
			return a.expression(n.left, func(condition int) int {
				return a.branch(condition, a.expression(n.right, join), a.expression(n.third, join))
			})
		})
	case 7:
		op := a.b.texts[a.b.tokens[n.token-1].text-1]
		if op == "&&" || op == "||" {
			return a.joined(next, func(join func(int) int) int {
				return a.expression(n.left, func(value int) int {
					return a.bind(value, func(left int) int {
						right := a.expression(n.right, join)
						unchanged := join(a.reference(left))
						if op == "&&" {
							return a.branch(a.reference(left), right, unchanged)
						}
						return a.branch(a.reference(left), unchanged, right)
					})
				})
			})
		}
		return a.expression(n.left, func(left int) int {
			return a.bind(left, func(symbol int) int {
				return a.expression(n.right, func(right int) int { n.left = a.reference(symbol); n.right = right; return next(a.operation(n)) })
			})
		})
	case 30: // nullish coalescing
		return a.joined(next, func(join func(int) int) int {
			return a.expression(n.left, func(value int) int {
				return a.bind(value, func(left int) int {
					return a.branch(a.nullish(left), a.expression(n.right, join), join(a.reference(left)))
				})
			})
		})
	case 32: // array literal
		return a.operands(a.list(n.children), true, func(values []int) int { n.children = a.link(values); return next(a.operation(n)) })
	case 4: // object literal
		return a.object(n, next)
	case 5: // property
		return a.expression(n.left, func(value int) int { n.left = value; return next(a.operation(n)) })
	case 33: // index
		return a.expression(n.left, func(left int) int {
			return a.bind(left, func(symbol int) int {
				return a.expression(n.right, func(right int) int { n.left = a.reference(symbol); n.right = right; return next(a.operation(n)) })
			})
		})
	case 6:
		return a.call(n, next)
	case 54: // constructor
		return a.expression(n.left, func(value int) int {
			return a.bind(value, func(target int) int {
				return a.operands(a.list(n.children), false, func(values []int) int {
					n.left = a.reference(target)
					n.children = a.link(values)
					return next(a.operation(n))
				})
			})
		})
	}
	a.b.unsupported(a.source)
	return 0
}

type asyncOperand struct {
	symbol int
	spread bool
	hole   int
}

func (a *asyncExpressionLowering) arrayType() int {
	a.b.types = append(a.b.types, irType{kind: 7, element: a.any})
	return len(a.b.types)
}
func (a *asyncExpressionLowering) snapshot(value int, object bool) int {
	spread := a.node(35, "await spread snapshot")
	a.b.nodes[spread-1].left = value
	kind := 32
	typ := a.arrayType()
	if object {
		kind = 4
		typ = a.any
	}
	result := a.node(kind, "await spread snapshot")
	a.b.nodes[result-1].typ = typ
	a.b.nodes[result-1].children = spread
	return result
}
func (a *asyncExpressionLowering) operands(values []int, holes bool, next func([]int) int) int {
	var walk func(int, []asyncOperand) int
	walk = func(index int, stored []asyncOperand) int {
		if index == len(values) {
			refs := make([]int, len(stored))
			for i, item := range stored {
				if item.hole != 0 {
					refs[i] = item.hole
					continue
				}
				refs[i] = a.reference(item.symbol)
				if item.spread {
					spread := a.node(35, "await spread")
					a.b.nodes[spread-1].left = refs[i]
					refs[i] = spread
				}
			}
			return next(refs)
		}
		id := values[index]
		n := a.b.nodes[id-1]
		if holes && n.kind == 20 && n.constant != 0 {
			return walk(index+1, append(stored, asyncOperand{hole: id}))
		}
		if n.kind == 35 {
			a.discard(id)
			return a.expression(n.left, func(value int) int {
				return a.bind(a.snapshot(value, false), func(symbol int) int { return walk(index+1, append(stored, asyncOperand{symbol: symbol, spread: true})) })
			})
		}
		return a.expression(id, func(value int) int {
			return a.bind(value, func(symbol int) int { return walk(index+1, append(stored, asyncOperand{symbol: symbol})) })
		})
	}
	return walk(0, nil)
}
func (a *asyncExpressionLowering) object(n irNode, next asyncExpressionContinuation) int {
	fields := a.list(n.children)
	var walk func(int, []irNode) int
	walk = func(index int, stored []irNode) int {
		if index == len(fields) {
			ids := make([]int, len(stored))
			for i, field := range stored {
				ids[i] = a.operation(field)
			}
			n.children = a.link(ids)
			return next(a.operation(n))
		}
		field := a.b.nodes[fields[index]-1]
		a.discard(fields[index])
		if field.kind == 35 {
			return a.expression(field.left, func(value int) int {
				return a.bind(a.snapshot(value, true), func(symbol int) int { field.left = a.reference(symbol); return walk(index+1, append(stored, field)) })
			})
		}
		value := func(key int) int {
			return a.expression(field.left, func(value int) int {
				return a.bind(value, func(symbol int) int {
					field.left = a.reference(symbol)
					field.right = key
					return walk(index+1, append(stored, field))
				})
			})
		}
		if field.right != 0 {
			return a.expression(field.right, func(key int) int {
				canonical := a.node(72, "await property key")
				a.b.nodes[canonical-1].typ = a.b.typ(a.b.c.GetStringType())
				a.b.nodes[canonical-1].left = key
				return a.bind(canonical, func(symbol int) int { return value(a.reference(symbol)) })
			})
		}
		return value(0)
	}
	return walk(0, nil)
}
func (a *asyncExpressionLowering) nullish(value int) int {
	equal := func(kind int) int {
		null := a.node(kind, "nullish value")
		comparison := a.node(7, "===")
		a.b.nodes[comparison-1].left = a.reference(value)
		a.b.nodes[comparison-1].right = null
		a.b.nodes[comparison-1].typ = a.b.typ(a.b.c.GetBooleanType())
		return comparison
	}
	test := a.node(7, "||")
	a.b.nodes[test-1].left = equal(21)
	a.b.nodes[test-1].right = equal(20)
	a.b.nodes[test-1].typ = a.b.typ(a.b.c.GetBooleanType())
	return test
}

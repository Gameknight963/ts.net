package main

import (
	"strconv"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
)

const rawAsyncAwait = -1

func expressionHasAwait(n *ast.Node) bool {
	if n == nil {
		return false
	}
	if ast.IsFunctionLike(n) {
		return expressionHasAwait(n.Name())
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
	asyncGenerator      bool
	b                   *irBuilder
	source              *ast.Node
	any                 int
	region              int
	functionRegions     map[int]int
	continuationRegions map[int]int
	transfers           []asyncTransfer
	cachedTransfers     map[int]int
}
type asyncTransfer struct{ node, symbol int }

func (a *asyncExpressionLowering) recordContinuation(symbol, value int) {
	if value != 0 && a.b.nodes[value-1].kind == 38 {
		if a.continuationRegions == nil {
			a.continuationRegions = map[int]int{}
		}
		a.continuationRegions[symbol] = a.functionRegions[a.b.nodes[value-1].left]
	}
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
	a.recordContinuation(symbol, value)
	binding := a.node(13, "await temporary")
	a.b.nodes[binding-1].typ = typ
	a.b.nodes[binding-1].symbol = symbol
	a.b.nodes[binding-1].name = a.b.text("let")
	a.b.nodes[binding-1].left = value
	return a.sequence(binding, next(symbol))
}
func (a *asyncExpressionLowering) functionParameters(count int, body func([]int) int) int {
	function := a.node(19, "await continuation")
	if a.functionRegions == nil {
		a.functionRegions = map[int]int{}
	}
	a.functionRegions[function] = a.region
	symbol := a.symbol(a.any)
	a.b.symbols[symbol-1].function = function
	a.b.nodes[function-1].symbol = symbol
	debugName := "resume"
	owner := enclosingAsyncFunction(a.source)
	if ast.IsFunctionLike(a.source) {
		owner = a.source
	}
	if owner != nil {
		if name := a.b.functionDebugName(owner); name != "" {
			debugName = name + "_resume"
		}
	}
	a.b.nodes[function-1].name = a.b.text(debugName)
	a.b.nodes[function-1].next = a.b.functions
	a.b.functions = function
	arguments := make([]int, count)
	parameters := make([]int, count)
	for i := range arguments {
		argument := a.symbol(a.any)
		arguments[i] = argument
		p := a.node(13, "await argument")
		a.b.nodes[p-1].symbol = argument
		parameters[i] = p
	}
	a.b.nodes[function-1].children = a.link(parameters)
	a.b.nodes[function-1].left = body(arguments)
	value := a.node(38, "await continuation")
	a.b.nodes[value-1].symbol = symbol
	a.b.nodes[value-1].left = function
	return value
}
func (a *asyncExpressionLowering) function(parameter bool, body func(int) int) int {
	count := 0
	if parameter {
		count = 1
	}
	return a.functionParameters(count, func(arguments []int) int {
		if parameter {
			return body(arguments[0])
		}
		return body(0)
	})
}

// Yield shares the same continuation and protected-region pass as await.
func (a *asyncExpressionLowering) hasAwait(id int) bool {
	if id == 0 {
		return false
	}
	n := a.b.nodes[id-1]
	if n.kind == rawAsyncAwait || n.kind == rawGeneratorYield || a.asyncGenerator && n.kind == 14 {
		return true
	}
	if n.kind == 34 && n.third != 0 {
		return true
	}
	// Jump targets point back into statements, rather than to child expressions.
	if n.kind == 38 || n.kind == 19 || n.kind == 25 || n.kind == 26 {
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
	arguments := a.list(n.children)
	return a.callTarget(n.left, func(target, receiver int) int {
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
	})
}
func (a *asyncExpressionLowering) callTarget(id int, next func(int, int) int) int {
	callee := a.b.nodes[id-1]
	if callee.kind == 62 {
		a.discard(id)
		return a.chain(callee, next)
	}
	if callee.kind == 5 || callee.kind == 33 {
		// Intrinsic call lowering uses synthetic names for console/performance
		// and Object.hasOwn. Awaited arguments must save actual namespace values.
		if callee.typ == 0 {
			callee.typ = a.any
		}
		owner := &a.b.nodes[callee.left-1]
		if owner.kind == 3 && owner.symbol == 0 {
			owner.kind = 66
			owner.typ = a.any
		}
		a.discard(id)
		return a.expression(callee.left, func(value int) int {
			return a.bind(value, func(receiver int) int {
				get := func(key int) int {
					callee.left = a.reference(receiver)
					if callee.kind == 33 {
						callee.right = key
					}
					return a.bind(a.operation(callee), func(target int) int { return next(target, receiver) })
				}
				if callee.kind == 33 {
					return a.expression(callee.right, get)
				}
				return get(0)
			})
		})
	}
	return a.expression(id, func(value int) int { return a.bind(value, func(target int) int { return next(target, 0) }) })
}
func (a *asyncExpressionLowering) expression(id int, next asyncExpressionContinuation) int {
	if !a.hasAwait(id) {
		return next(id)
	}
	n := a.b.nodes[id-1]
	a.discard(id)
	switch n.kind {
	case rawGeneratorYield:
		return a.expression(n.left, func(value int) int {
			if value == 0 {
				value = a.node(20, "undefined")
			}
			callback := a.function(true, func(parameter int) int { return next(a.reference(parameter)) })
			name := "__generatorYield"
			if n.constant != 0 {
				name = "__generatorDelegate"
				if a.asyncGenerator {
					name = "__asyncGeneratorDelegate"
				}
			}
			return a.returned(a.b.builtinCall(a.source, name, value, callback))
		})
	case rawAsyncAwait:
		return a.expression(n.left, func(value int) int {
			callback := a.function(true, func(parameter int) int { return next(a.reference(parameter)) })
			await := a.node(70, "await")
			a.b.nodes[await-1].left = value
			a.b.nodes[await-1].right = callback
			return a.returned(await)
		})
	case 8, 9, 23: // unary, assertion, typeof
		if n.kind == 8 && a.b.texts[a.b.tokens[n.token-1].text-1] == "delete" {
			if n.constant != 0 {
				target := a.b.nodes[n.left-1]
				a.discard(n.left)
				return a.expression(target.right, func(key int) int { target.right = key; n.left = a.operation(target); return next(a.operation(n)) })
			}
			return a.target(n.left, func(target func() int) int { n.left = target(); return next(a.operation(n)) })
		}
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
	case 62: // optional chain
		return a.chain(n, func(value, receiver int) int { return next(a.reference(value)) })
	case 10: // assignment
		return a.assignment(n, next)
	case 11: // prefix/postfix update with an awaited reference
		return a.target(n.left, func(target func() int) int { n.left = target(); return next(a.operation(n)) })
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
	case 36: // destructuring assignment; preserve the original RHS value
		return a.expression(n.left, func(value int) int {
			return a.bind(value, func(original int) int {
				return a.pattern(n, a.reference(original), func() int { return next(a.reference(original)) })
			})
		})
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
	case 55:
		return a.classExpression(n, next)
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

// Capture the reference, not its current value. Each use gets fresh IR nodes so
// closure and ownership walks see an ordinary tree.
func (a *asyncExpressionLowering) target(id int, next func(func() int) int) int {
	n := a.b.nodes[id-1]
	a.discard(id)
	switch n.kind {
	case 9: // assertions do not change the referenced storage location
		return a.target(n.left, next)
	case 3:
		return next(func() int { return a.operation(n) })
	case 5, 33:
		return a.expression(n.left, func(value int) int {
			return a.bind(value, func(receiver int) int {
				finish := func(key int) int {
					return next(func() int {
						target := n
						target.left = a.reference(receiver)
						if key != 0 {
							target.right = a.reference(key)
						}
						return a.operation(target)
					})
				}
				if n.kind == 5 {
					return finish(0)
				}
				// A reference keeps the raw key. GetValue and PutValue each
				// perform their own conversion, potentially on opposite sides of await.
				return a.expression(n.right, func(value int) int { return a.bind(value, finish) })
			})
		})
	}
	a.b.unsupported(a.source)
	return 0
}
func (a *asyncExpressionLowering) assignment(n irNode, next asyncExpressionContinuation) int {
	op := a.b.texts[a.b.tokens[n.token-1].text-1]
	return a.target(n.left, func(target func() int) int {
		store := func(value int) int {
			assignment := a.node(10, "=")
			a.b.nodes[assignment-1].typ = n.typ
			a.b.nodes[assignment-1].left = target()
			a.b.nodes[assignment-1].right = value
			return next(assignment)
		}
		if op == "=" {
			return a.expression(n.right, store)
		}
		return a.bind(target(), func(previous int) int {
			if op == "&&=" || op == "||=" || op == "??=" {
				return a.joined(next, func(join func(int) int) int {
					assign := a.expression(n.right, func(value int) int {
						assignment := a.node(10, "=")
						a.b.nodes[assignment-1].typ = n.typ
						a.b.nodes[assignment-1].left = target()
						a.b.nodes[assignment-1].right = value
						return join(assignment)
					})
					unchanged := join(a.reference(previous))
					if op == "&&=" {
						return a.branch(a.reference(previous), assign, unchanged)
					}
					if op == "||=" {
						return a.branch(a.reference(previous), unchanged, assign)
					}
					return a.branch(a.nullish(previous), assign, unchanged)
				})
			}
			return a.expression(n.right, func(value int) int {
				binary := a.node(7, strings.TrimSuffix(op, "="))
				a.b.nodes[binary-1].typ = n.typ
				a.b.nodes[binary-1].left = a.reference(previous)
				a.b.nodes[binary-1].right = value
				return store(binary)
			})
		})
	})
}

// Share both the value and the receiver across optional branches. Calls following
// a parenthesized chain still use the receiver of its last property access.
func (a *asyncExpressionLowering) chain(n irNode, next func(int, int) int) int {
	steps := a.list(n.children)
	continuation := a.functionParameters(2, func(arguments []int) int { return next(arguments[0], arguments[1]) })
	return a.bind(continuation, func(join int) int {
		finish := func(value, receiver int) int {
			call := a.node(6, "await chain join")
			a.b.nodes[call-1].left = a.reference(join)
			args := []int{value, receiver}
			a.b.nodes[call-1].children = a.link(args)
			return a.returned(call)
		}
		var walk func(int, int, int) int
		walk = func(index, value, receiver int) int {
			if index == len(steps) {
				this := a.node(20, "undefined")
				if receiver != 0 {
					this = a.reference(receiver)
				}
				return finish(a.reference(value), this)
			}
			step := a.b.nodes[steps[index]-1]
			a.discard(steps[index])
			advance := func() int {
				if step.kind == 65 {
					return a.operands(a.list(step.children), false, func(args []int) int {
						call := a.node(71, "await chain call")
						a.b.nodes[call-1].left = a.reference(value)
						if receiver != 0 {
							a.b.nodes[call-1].right = a.reference(receiver)
						}
						a.b.nodes[call-1].children = a.link(args)
						return a.bind(call, func(result int) int { return walk(index+1, result, 0) })
					})
				}
				get := func(key int) int {
					step.constant = 0
					step.right = key
					child := a.operation(step)
					root := a.node(62, "await chain property")
					a.b.nodes[root-1].typ = step.typ
					if n.constant != 0 && index == len(steps)-1 {
						a.b.nodes[root-1].constant = 1
						a.b.nodes[root-1].typ = n.typ
					}
					a.b.nodes[root-1].left = a.reference(value)
					a.b.nodes[root-1].children = child
					return a.bind(root, func(result int) int { return walk(index+1, result, value) })
				}
				if step.kind == 64 {
					return a.expression(step.right, get)
				}
				return get(0)
			}
			if step.constant != 0 {
				missing := a.node(20, "undefined")
				if n.constant != 0 {
					missing = a.node(2, "true")
					a.b.nodes[missing-1].typ = n.typ
				}
				return a.branch(a.nullish(value), finish(missing, a.node(20, "undefined")), advance())
			}
			return advance()
		}
		if len(steps) > 0 && a.b.nodes[steps[0]-1].kind == 65 {
			return a.callTarget(n.left, func(value, receiver int) int { return walk(0, value, receiver) })
		}
		return a.expression(n.left, func(base int) int { return a.bind(base, func(value int) int { return walk(0, value, 0) }) })
	})
}

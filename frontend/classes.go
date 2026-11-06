package main

import (
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
)

func (b *irBuilder) className(n *ast.Node) string {
	if n.Name() != nil {
		return n.Name().Text()
	}
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsDefault != 0 {
		return "default"
	}
	parent := n.Parent
	for parent != nil && (parent.Kind == ast.KindParenthesizedExpression || parent.Kind == ast.KindAsExpression || parent.Kind == ast.KindSatisfiesExpression) {
		parent = parent.Parent
	}
	if parent != nil && (parent.Kind == ast.KindVariableDeclaration || parent.Kind == ast.KindPropertyAssignment || parent.Kind == ast.KindPropertyDeclaration) {
		if parent.Name().Kind == ast.KindPrivateIdentifier {
			return parent.Name().Text()
		}
		if name, known := b.literalPropertyName(parent.Name()); known {
			if parent.Kind == ast.KindPropertyDeclaration && ast.GetCombinedModifierFlags(parent)&ast.ModifierFlagsAccessor != 0 {
				return "#" + name + "_accessor_storage"
			}
			return name
		}
	}
	return ""
}
func (b *irBuilder) newFunction(n *ast.Node, name string) (int, int) {
	typ := b.typ(b.c.GetAnyType())
	function := b.node(19, n, name)
	b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: typ, function: function})
	symbol := len(b.symbols)
	b.nodes[function-1].symbol = symbol
	b.nodes[function-1].typ = typ
	b.nodes[function-1].next = b.functions
	b.functions = function
	value := b.node(38, n, "function")
	b.nodes[value-1].symbol = symbol
	b.nodes[value-1].typ = typ
	b.nodes[value-1].left = function
	return function, value
}
func (b *irBuilder) classReference(n *ast.Node, symbol int) int {
	id := b.node(3, n, "class")
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	return id
}
func (b *irBuilder) classInitializer(n *ast.Node, static bool) int {
	function, value := b.newFunction(n, "")
	previous, scope := b.thisSymbol, b.thisScope
	previousNewTarget := b.newTargetUsed
	previousArguments := b.argumentsUsed
	b.newTargetUsed = false
	b.argumentsUsed = false
	previousStatic := b.superStatic
	b.superStatic = static
	b.symbols = append(b.symbols, irSymbol{name: b.text("this"), typ: b.typ(b.c.GetAnyType())})
	b.thisSymbol = len(b.symbols)
	b.thisScope = true
	parameter := b.node(13, n, "this")
	b.nodes[parameter-1].symbol = b.thisSymbol
	b.nodes[parameter-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[function-1].fourth = parameter
	b.nodes[function-1].children = parameter
	body := b.node(12, n, "fields")
	b.nodes[function-1].left = body
	last := 0
	brands := map[int]bool{}
	for _, member := range n.Members() {
		if member.Name() == nil || member.Name().Kind != ast.KindPrivateIdentifier || privateKind(member) == 0 || (ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0) != static {
			continue
		}
		if member.Kind != ast.KindPropertyDeclaration && member.Body() == nil {
			continue
		}
		symbol := b.privateMembers[b.c.GetSymbolAtLocation(member.Name())]
		if brands[symbol] {
			continue
		}
		brands[symbol] = true
		empty := b.node(20, member, "undefined")
		b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
		stamp := b.privateInitialization(member, b.thisSymbol, empty, 1)
		if last == 0 {
			b.nodes[body-1].children = stamp
		} else {
			b.nodes[last-1].next = stamp
		}
		last = stamp
	}
	for _, member := range n.Members() {
		flags := ast.GetCombinedModifierFlags(member)
		if flags&ast.ModifierFlagsDecorator != 0 {
			b.unsupported(member)
		}
		if member.Kind == ast.KindClassStaticBlockDeclaration {
			if static {
				statement := b.lower(member.AsClassStaticBlockDeclaration().Body)
				if last == 0 {
					b.nodes[body-1].children = statement
				} else {
					b.nodes[last-1].next = statement
				}
				last = statement
			}
			continue
		}
		if (flags&ast.ModifierFlagsStatic != 0) != static {
			continue
		}
		if member.Kind != ast.KindPropertyDeclaration {
			continue
		}
		if flags&(ast.ModifierFlagsAmbient|ast.ModifierFlagsAbstract) != 0 {
			continue
		}
		name, known := b.literalPropertyName(member.Name())
		if member.Name().Kind == ast.KindPrivateIdentifier {
			expression := b.lower(member.Initializer())
			if expression == 0 {
				expression = b.node(20, member, "undefined")
				b.nodes[expression-1].typ = b.typ(b.c.GetUndefinedType())
			}
			mode := 0
			if privateKind(member) == 4 {
				mode = 2
			}
			statement := b.privateInitialization(member, b.thisSymbol, expression, mode)
			if last == 0 {
				b.nodes[body-1].children = statement
			} else {
				b.nodes[last-1].next = statement
			}
			last = statement
			continue
		}
		statement := b.node(57, member, "field")
		if known {
			b.nodes[statement-1].name = b.text(name)
		} else {
			key := b.classReference(member, b.classFieldKeys[member])
			b.nodes[statement-1].third = key
		}
		if slot := b.classAutoSlots[member]; slot != 0 {
			ref := b.classReference(member, slot)
			b.nodes[statement-1].fourth = ref
		}
		if anonymousPropertyFunction(member.Initializer()) {
			b.nodes[statement-1].constant = 1
		}
		receiver := b.classReference(member, b.thisSymbol)
		b.nodes[statement-1].left = receiver
		expression := b.lower(member.Initializer())
		if expression == 0 {
			expression = b.node(20, member, "undefined")
			b.nodes[expression-1].typ = b.typ(b.c.GetUndefinedType())
		}
		b.nodes[statement-1].right = expression
		if last == 0 {
			b.nodes[body-1].children = statement
		} else {
			b.nodes[last-1].next = statement
		}
		last = statement
	}
	b.thisSymbol, b.thisScope = previous, scope
	b.newTargetUsed = previousNewTarget
	b.argumentsUsed = previousArguments
	b.superStatic = previousStatic
	return value
}
func (b *irBuilder) classValue(n *ast.Node) int {
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsDecorator != 0 {
		b.unsupported(n)
	}
	if ast.HasAmbientModifier(n) {
		return 0
	}
	var parent *ast.Node
	if n.ClassLikeData().HeritageClauses != nil {
		for _, clause := range n.ClassLikeData().HeritageClauses.Nodes {
			if clause.AsHeritageClause().Token == ast.KindExtendsKeyword {
				parent = clause.AsHeritageClause().Types.Nodes[0].Expression()
			}
		}
	}
	name := b.className(n)
	typ := b.typ(b.c.GetAnyType())
	classSymbol := n.Symbol()
	instance := b.typ(b.c.GetDeclaredTypeOfSymbol(classSymbol))
	outer := 0
	if n.Kind == ast.KindClassDeclaration {
		if n.Name() != nil {
			outer = b.symbol(n.Name())
		} else {
			outer = b.symbolFor(n.Symbol(), "default")
		}
	}
	factory, factoryValue := b.newFunction(n, "")
	b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: typ})
	binding := len(b.symbols)
	previous, had := b.symbolIDs[classSymbol]
	b.symbolIDs[classSymbol] = binding
	base := b.lower(parent)
	previousSuper, previousStatic := b.superSymbol, b.superStatic
	previousKeySuper, previousKeyStatic := b.classKeySuper, b.classKeyStatic
	b.classKeySuper, b.classKeyStatic = previousSuper, previousStatic
	b.superSymbol = binding
	b.superStatic = false
	if b.classFieldKeys == nil {
		b.classFieldKeys = map[*ast.Node]int{}
	}
	// Initializers capture one stored key per class definition. Evaluate the
	// expressions later in the member list, alongside methods and accessors.
	keyFirst, keyLast := 0, 0
	if b.classAutoSlots == nil {
		b.classAutoSlots = map[*ast.Node]int{}
	}
	declare := func(member *ast.Node, name string, typ int) int {
		b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: typ})
		symbol := len(b.symbols)
		declaration := b.node(13, member, name)
		b.nodes[declaration-1].symbol = symbol
		b.nodes[declaration-1].typ = typ
		empty := b.node(20, member, "undefined")
		b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
		b.nodes[declaration-1].left = empty
		if keyLast == 0 {
			keyFirst = declaration
		} else {
			b.nodes[keyLast-1].next = declaration
		}
		keyLast = declaration
		return symbol
	}
	if b.privateMembers == nil {
		b.privateMembers = map[*ast.Symbol]int{}
	}
	for _, member := range n.Members() {
		if member.Name() != nil && member.Name().Kind == ast.KindPrivateIdentifier {
			symbol := b.c.GetSymbolAtLocation(member.Name())
			if b.privateMembers[symbol] == 0 {
				b.privateMembers[symbol] = declare(member, "$private", typ)
			}
			continue
		}
		if member.Kind != ast.KindPropertyDeclaration || ast.GetCombinedModifierFlags(member)&(ast.ModifierFlagsAmbient|ast.ModifierFlagsAbstract) != 0 {
			continue
		}
		if ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsAccessor != 0 {
			b.classAutoSlots[member] = declare(member, "$accessor", typ)
		}
		if _, known := b.literalPropertyName(member.Name()); !known {
			if member.Name().Kind != ast.KindComputedPropertyName {
				b.unsupported(member)
			}
			b.classFieldKeys[member] = declare(member, "$key", b.typ(b.c.GetStringType()))
		}
	}

	value := b.node(55, n, name)
	b.nodes[value-1].name = b.text(name)
	b.nodes[value-1].typ = typ
	b.nodes[value-1].third = base
	// The instance type is carried by a marker node so the compiler can select
	// its structural CLR class without adding a second type field to the IR.
	marker := b.node(20, n, "instance")
	b.nodes[marker-1].typ = instance
	if n.Kind == ast.KindClassExpression && n.Name() == nil {
		owner := n.Parent
		for owner != nil && (owner.Kind == ast.KindParenthesizedExpression || owner.Kind == ast.KindAsExpression || owner.Kind == ast.KindSatisfiesExpression || owner.Kind == ast.KindTypeAssertionExpression) {
			owner = owner.Parent
		}
		if key := b.classFieldKeys[owner]; key != 0 {
			reference := b.classReference(n, key)
			if ast.GetCombinedModifierFlags(owner)&ast.ModifierFlagsAccessor != 0 {
				stringType := b.typ(b.c.GetStringType())
				prefix := b.node(1, n, "#")
				b.nodes[prefix-1].typ = stringType
				start := b.node(7, n, "+")
				b.nodes[start-1].left = prefix
				b.nodes[start-1].right = reference
				b.nodes[start-1].typ = stringType
				suffix := b.node(1, n, "_accessor_storage")
				b.nodes[suffix-1].typ = stringType
				end := b.node(7, n, "+")
				b.nodes[end-1].left = start
				b.nodes[end-1].right = suffix
				b.nodes[end-1].typ = stringType
				reference = end
			}
			b.nodes[marker-1].left = reference
		}
	}
	b.nodes[value-1].fourth = marker
	fields := b.classInitializer(n, false)
	b.nodes[value-1].left = fields
	staticFields := b.classInitializer(n, true)
	last := 0
	for _, member := range n.Members() {
		if member.Name() != nil && member.Name().Kind == ast.KindPrivateIdentifier {
			if member.Kind != ast.KindPropertyDeclaration && member.Body() == nil {
				continue
			}
			definition := b.node(5, member, "private definition")
			b.nodes[definition-1].name = b.text(member.Name().Text())
			b.nodes[definition-1].symbol = b.privateMembers[b.c.GetSymbolAtLocation(member.Name())]
			marker := b.node(0, member, strconv.Itoa(privateKind(member)))
			b.nodes[marker-1].typ = b.typ(b.c.GetNumberType())
			b.nodes[definition-1].fourth = marker
			slot := b.privateReference(member.Name())
			b.nodes[definition-1].right = slot
			if member.Kind != ast.KindPropertyDeclaration {
				b.superStatic = ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0
				fn := b.functionValue(member)
				b.nodes[definition-1].left = fn
			}
			if last == 0 {
				b.nodes[value-1].children = definition
			} else {
				b.nodes[last-1].next = definition
			}
			last = definition
			continue
		}
		switch member.Kind {
		case ast.KindConstructor:
			if member.Body() != nil {
				b.superStatic = false
				ctor := b.functionValue(member)
				b.nodes[value-1].right = ctor
				// Parameter properties initialize after defaults and fields.
				function := b.nodes[ctor-1].left
				first := 0
				tail := 0
				for _, parameter := range member.Parameters() {
					if ast.GetCombinedModifierFlags(parameter)&ast.ModifierFlagsParameterPropertyModifier == 0 {
						continue
					}
					if parameter.Name().Kind != ast.KindIdentifier {
						b.unsupported(parameter)
					}
					if b.nodes[function-1].fourth == 0 {
						b.symbols = append(b.symbols, irSymbol{name: b.text("this"), typ: typ})
						hidden := b.node(13, member, "this")
						b.nodes[hidden-1].symbol = len(b.symbols)
						b.nodes[hidden-1].typ = typ
						b.nodes[hidden-1].next = b.nodes[function-1].children
						b.nodes[function-1].children = hidden
						b.nodes[function-1].fourth = hidden
					}
					field := b.node(57, parameter, "field")
					b.nodes[field-1].name = b.text(parameter.Name().Text())
					receiver := b.classReference(parameter, b.nodes[b.nodes[function-1].fourth-1].symbol)
					b.nodes[field-1].left = receiver
					source := b.lower(parameter.Name())
					b.nodes[field-1].right = source
					if first == 0 {
						first = field
					} else {
						b.nodes[tail-1].next = field
					}
					tail = field
				}
				if first != 0 {
					body := b.nodes[function-1].left
					if parent != nil {
						callback, callbackValue := b.newFunction(member, "")
						callbackBody := b.node(12, member, "parameter properties")
						b.nodes[callbackBody-1].children = first
						b.nodes[callback-1].left = callbackBody
						setup := b.node(61, member, "parameter properties")
						b.nodes[setup-1].left = b.classReference(member, b.nodes[b.nodes[function-1].fourth-1].symbol)
						b.nodes[setup-1].right = callbackValue
						b.nodes[setup-1].next = b.nodes[body-1].children
						b.nodes[body-1].children = setup
					} else {
						b.nodes[tail-1].next = b.nodes[body-1].children
						b.nodes[body-1].children = first
					}
				}
			}
		case ast.KindMethodDeclaration, ast.KindGetAccessor, ast.KindSetAccessor:
			if member.Body() == nil {
				continue
			}
			b.superStatic = ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0
			method := b.lower(member)
			if ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0 {
				b.nodes[method-1].constant = 1
			} else {
				b.nodes[method-1].constant = 0
			}
			if last == 0 {
				b.nodes[value-1].children = method
			} else {
				b.nodes[last-1].next = method
			}
			last = method
		case ast.KindPropertyDeclaration:
			if symbol := b.classFieldKeys[member]; symbol != 0 {
				b.superSymbol, b.superStatic = previousSuper, previousStatic
				expression := b.lower(member.Name().Expression())
				b.superSymbol, b.superStatic = binding, false
				key := b.node(13, member, "computed field key")
				b.nodes[key-1].symbol = symbol
				b.nodes[key-1].typ = b.symbols[symbol-1].typ
				b.nodes[key-1].left = expression
				if last == 0 {
					b.nodes[value-1].children = key
				} else {
					b.nodes[last-1].next = key
				}
				last = key
			}
			if symbol := b.classAutoSlots[member]; symbol != 0 {
				accessor := b.node(5, member, "auto-accessor")
				b.nodes[accessor-1].symbol = symbol
				b.nodes[accessor-1].constant = 0
				if ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0 {
					b.nodes[accessor-1].constant = 1
				}
				if key := b.classFieldKeys[member]; key != 0 {
					ref := b.classReference(member, key)
					b.nodes[accessor-1].right = ref
				} else {
					name, _ := b.literalPropertyName(member.Name())
					b.nodes[accessor-1].name = b.text(name)
				}
				if last == 0 {
					b.nodes[value-1].children = accessor
				} else {
					b.nodes[last-1].next = accessor
				}
				last = accessor
			}
		case ast.KindSemicolonClassElement, ast.KindClassStaticBlockDeclaration:
		default:
			b.unsupported(member)
		}
	}
	b.superSymbol, b.superStatic = previousSuper, previousStatic
	b.classKeySuper, b.classKeyStatic = previousKeySuper, previousKeyStatic
	if had {
		b.symbolIDs[classSymbol] = previous
	} else {
		delete(b.symbolIDs, classSymbol)
	}
	body := b.node(12, n, "class scope")
	b.nodes[body-1].name = b.text("scope")
	b.nodes[factory-1].left = body
	variable := b.node(13, n, name)
	b.nodes[variable-1].symbol = binding
	b.nodes[variable-1].typ = typ
	b.nodes[variable-1].left = value
	initialize := b.node(56, n, "static fields")
	reference := b.classReference(n, binding)
	b.nodes[initialize-1].left = reference
	b.nodes[initialize-1].right = staticFields
	result := b.node(14, n, "return")
	reference = b.classReference(n, binding)
	b.nodes[result-1].left = reference
	if keyLast != 0 {
		b.nodes[body-1].children = keyFirst
		b.nodes[keyLast-1].next = variable
	} else {
		b.nodes[body-1].children = variable
	}
	b.nodes[variable-1].next = initialize
	b.nodes[initialize-1].next = result
	call := b.node(6, n, "class definition")
	b.nodes[call-1].left = factoryValue
	b.nodes[call-1].typ = typ
	if n.Kind == ast.KindClassExpression {
		return call
	}
	declaration := b.node(13, n, name)
	b.nodes[declaration-1].symbol = outer
	b.nodes[declaration-1].typ = typ
	b.nodes[declaration-1].left = call
	return declaration
}

func (b *irBuilder) rawThis(n *ast.Node) int {
	if b.thisSymbol == 0 {
		b.symbols = append(b.symbols, irSymbol{name: b.text("this"), typ: b.typ(b.c.GetAnyType())})
		b.thisSymbol = len(b.symbols)
	}
	return b.classReference(n, b.thisSymbol)
}

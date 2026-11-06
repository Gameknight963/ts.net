package main

import "github.com/microsoft/typescript-go/internal/ast"

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
		if name, known := b.literalPropertyName(parent.Name()); known {
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
	for _, member := range n.Members() {
		flags := ast.GetCombinedModifierFlags(member)
		if flags&(ast.ModifierFlagsDecorator|ast.ModifierFlagsAccessor) != 0 {
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
		if !known || member.Name().Kind == ast.KindPrivateIdentifier {
			b.unsupported(member)
		}
		statement := b.node(57, member, "field")
		b.nodes[statement-1].name = b.text(name)
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
	return value
}
func (b *irBuilder) classValue(n *ast.Node) int {
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsDecorator != 0 {
		b.unsupported(n)
	}
	if ast.HasAmbientModifier(n) {
		return 0
	}
	if n.ClassLikeData().HeritageClauses != nil {
		for _, clause := range n.ClassLikeData().HeritageClauses.Nodes {
			if clause.AsHeritageClause().Token == ast.KindExtendsKeyword {
				b.unsupported(clause)
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
	value := b.node(55, n, name)
	b.nodes[value-1].name = b.text(name)
	b.nodes[value-1].typ = typ
	// The instance type is carried by a marker node so the compiler can select
	// its structural CLR class without adding a second type field to the IR.
	marker := b.node(20, n, "instance")
	b.nodes[marker-1].typ = instance
	b.nodes[value-1].fourth = marker
	fields := b.classInitializer(n, false)
	b.nodes[value-1].left = fields
	staticFields := b.classInitializer(n, true)
	last := 0
	for _, member := range n.Members() {
		switch member.Kind {
		case ast.KindConstructor:
			if member.Body() != nil {
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
					b.nodes[tail-1].next = b.nodes[body-1].children
					b.nodes[body-1].children = first
				}
			}
		case ast.KindMethodDeclaration:
			if member.Body() == nil {
				continue
			}
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
		case ast.KindPropertyDeclaration, ast.KindSemicolonClassElement, ast.KindClassStaticBlockDeclaration:
		default:
			b.unsupported(member)
		}
	}
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
	b.nodes[body-1].children = variable
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

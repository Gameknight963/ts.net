package main

import (
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
)

type classDecoration struct {
	plan, alias, home, prefix int
	members                   []*ast.Node
	indices                   map[*ast.Node]int
}

func (b *irBuilder) decoratorNumber(n *ast.Node, value int) int {
	id := b.node(0, n, strconv.Itoa(value))
	b.nodes[id-1].typ = b.typ(b.c.GetNumberType())
	return id
}
func (b *irBuilder) decoratorText(n *ast.Node, value string) int {
	id := b.node(1, n, value)
	b.nodes[id-1].typ = b.typ(b.c.GetStringType())
	return id
}
func (b *irBuilder) decoratorArray(n *ast.Node) int {
	a := asyncExpressionLowering{b: b, source: n, any: b.typ(b.c.GetAnyType())}
	id := b.node(32, n, "decorators")
	b.nodes[id-1].typ = a.arrayType()
	last := 0
	for _, decorator := range n.Decorators() {
		value := b.lower(decorator.Expression())
		if last == 0 {
			b.nodes[id-1].children = value
		} else {
			b.nodes[last-1].next = value
		}
		last = value
	}
	return id
}
func (b *irBuilder) decoratorSymbol(name string) int {
	b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: b.typ(b.c.GetAnyType())})
	return len(b.symbols)
}
func (b *irBuilder) prepareClassDecoration(n *ast.Node, home int) *classDecoration {
	state := &classDecoration{home: home, indices: map[*ast.Node]int{}}
	for _, member := range n.Members() {
		if len(member.Decorators()) != 0 {
			state.indices[member] = len(state.members)
			state.members = append(state.members, member)
		}
	}
	if len(n.Decorators()) == 0 && len(state.members) == 0 {
		return nil
	}
	state.plan = b.decoratorSymbol("$decorators")
	state.alias = b.decoratorSymbol("$decoratedClass")
	state.prefix = b.node(13, n, "decorator state")
	b.nodes[state.prefix-1].symbol = state.plan
	b.nodes[state.prefix-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[state.prefix-1].left = b.builtinCall(n, "__decoratorPlan", b.decoratorArray(n))
	if b.classDecorations == nil {
		b.classDecorations = map[*ast.Node]*classDecoration{}
	}
	b.classDecorations[n] = state
	return state
}
func (b *irBuilder) storeMemberDecorators(n, member *ast.Node) int {
	state := b.classDecorations[n]
	if state == nil {
		return 0
	}
	index, ok := state.indices[member]
	if !ok {
		return 0
	}
	return b.expressionStatement(member, b.builtinCall(member, "__decoratorStore", b.classReference(member, state.plan), b.decoratorNumber(member, index), b.decoratorArray(member)))
}
func (b *irBuilder) decoratedFieldValue(n, member *ast.Node, receiver, value int) int {
	state := b.classDecorations[n]
	if state == nil {
		return value
	}
	index, ok := state.indices[member]
	if !ok {
		return value
	}
	return b.builtinCall(member, "__decoratorInit", b.classReference(member, state.plan), b.decoratorNumber(member, index), b.classReference(member, receiver), value)
}
func (b *irBuilder) decoratedFieldEnd(n, member *ast.Node, receiver int) int {
	state := b.classDecorations[n]
	if state == nil {
		return 0
	}
	index, ok := state.indices[member]
	if !ok {
		return 0
	}
	return b.expressionStatement(member, b.builtinCall(member, "__decoratorAfter", b.classReference(member, state.plan), b.decoratorNumber(member, index), b.classReference(member, receiver)))
}
func decoratorKind(member *ast.Node) string {
	switch member.Kind {
	case ast.KindMethodDeclaration:
		return "method"
	case ast.KindGetAccessor:
		return "getter"
	case ast.KindSetAccessor:
		return "setter"
	}
	if ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsAccessor != 0 {
		return "accessor"
	}
	return "field"
}
func (b *irBuilder) applyClassDecoration(n *ast.Node, state *classDecoration, after int) int {
	last := after
	for index, member := range state.members {
		var key, private int
		if member.Name().Kind == ast.KindPrivateIdentifier {
			key = b.decoratorText(member, member.Name().Text())
			private = b.privateReference(member.Name())
		} else {
			if symbol := b.classFieldKeys[member]; symbol != 0 {
				key = b.classReference(member, symbol)
			} else {
				name, _ := b.literalPropertyName(member.Name())
				key = b.decoratorText(member, name)
			}
			private = b.node(20, member, "undefined")
			b.nodes[private-1].typ = b.typ(b.c.GetUndefinedType())
		}
		static := 0
		if ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0 {
			static = 1
		}
		configure := b.expressionStatement(member, b.builtinCall(member, "__decoratorMember", b.classReference(member, state.plan), b.decoratorNumber(member, index), b.decoratorText(member, decoratorKind(member)), key, b.decoratorNumber(member, static), private))
		b.nodes[last-1].next = configure
		last = configure
	}
	alias := b.node(13, n, "decorated class")
	b.nodes[alias-1].symbol = state.alias
	b.nodes[alias-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[alias-1].left = b.builtinCall(n, "__decoratorApply", b.classReference(n, state.plan), b.classReference(n, state.home))
	b.nodes[last-1].next = alias
	return alias
}

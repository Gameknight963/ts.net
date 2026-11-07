package main

import "github.com/microsoft/typescript-go/internal/ast"

func legacyMemberDecorated(n *ast.Node) bool {
	if len(n.Decorators()) != 0 {
		return true
	}
	if ast.IsFunctionLike(n) {
		for _, parameter := range n.Parameters() {
			if len(parameter.Decorators()) != 0 {
				return true
			}
		}
	}
	return false
}
func legacyClassDecorated(n *ast.Node) bool {
	if len(n.Decorators()) != 0 {
		return true
	}
	for _, member := range n.Members() {
		if legacyMemberDecorated(member) {
			return true
		}
	}
	return false
}
func (b *irBuilder) legacyDecoratorArray(n *ast.Node, constructor *ast.Node) int {
	array := b.decoratorArray(n)
	last := b.nodes[array-1].children
	for last != 0 && b.nodes[last-1].next != 0 {
		last = b.nodes[last-1].next
	}
	parameters := n
	if constructor != nil {
		parameters = constructor
	}
	if ast.IsFunctionLike(parameters) {
		for index, parameter := range parameters.Parameters() {
			for _, decorator := range parameter.Decorators() {
				value := b.builtinCall(decorator, "__legacyParam", b.decoratorNumber(parameter, index), b.lower(decorator.Expression()))
				if last == 0 {
					b.nodes[array-1].children = value
				} else {
					b.nodes[last-1].next = value
				}
				last = value
			}
		}
	}
	return array
}
func (b *irBuilder) applyLegacyDecorators(n *ast.Node, home, alias, after int) int {
	last := after
	for _, static := range []bool{false, true} {
		for _, member := range n.Members() {
			if member.Kind == ast.KindConstructor || !legacyMemberDecorated(member) || (ast.GetCombinedModifierFlags(member)&ast.ModifierFlagsStatic != 0) != static {
				continue
			}
			key := 0
			if symbol := b.classFieldKeys[member]; symbol != 0 {
				key = b.classReference(member, symbol)
			} else {
				name, known := b.literalPropertyName(member.Name())
				if !known {
					b.unsupported(member)
				}
				key = b.decoratorText(member, name)
			}
			flags := 0
			if static {
				flags |= 1
			}
			if member.Kind == ast.KindPropertyDeclaration {
				flags |= 2
			}
			statement := b.expressionStatement(member, b.builtinCall(member, "__legacyMember", b.classReference(member, home), key, b.decoratorNumber(member, flags), b.legacyDecoratorArray(member, nil)))
			b.nodes[last-1].next = statement
			last = statement
		}
	}
	var constructor *ast.Node
	for _, member := range n.Members() {
		if member.Kind == ast.KindConstructor && member.Body() != nil {
			constructor = member
			break
		}
	}
	if len(n.Decorators()) != 0 || constructor != nil && legacyMemberDecorated(constructor) {
		assignment := b.node(10, n, "=")
		b.nodes[assignment-1].typ = b.typ(b.c.GetAnyType())
		b.nodes[assignment-1].left = b.classReference(n, alias)
		b.nodes[assignment-1].right = b.builtinCall(n, "__legacyClass", b.classReference(n, alias), b.legacyDecoratorArray(n, constructor))
		statement := b.expressionStatement(n, assignment)
		b.nodes[last-1].next = statement
		last = statement
	}
	return last
}

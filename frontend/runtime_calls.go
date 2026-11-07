package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) builtinCall(n *ast.Node, name string, arguments ...int) int {
	callee := b.node(66, n, name)
	b.nodes[callee-1].typ = b.typ(b.c.GetAnyType())
	id := b.node(6, n, "(")
	b.nodes[id-1].left = callee
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	for index, argument := range arguments {
		if index == 0 {
			b.nodes[id-1].children = argument
		} else {
			b.nodes[arguments[index-1]-1].next = argument
		}
	}
	return id
}
func (b *irBuilder) expressionStatement(n *ast.Node, expression int) int {
	id := b.node(18, n, ";")
	b.nodes[id-1].left = expression
	return id
}

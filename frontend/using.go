package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) hasUsingDeclarations(statements []*ast.Node) bool {
	for _, statement := range statements {
		if statement.Kind == ast.KindVariableStatement && ast.GetCombinedNodeFlags(statement.AsVariableStatement().DeclarationList)&ast.NodeFlagsUsing != 0 {
			return true
		}
	}
	return false
}
func (b *irBuilder) newUsingStack() int {
	b.symbols = append(b.symbols, irSymbol{name: b.text("using resources"), typ: b.typ(b.c.GetAnyType())})
	return len(b.symbols)
}
func (b *irBuilder) usingReference(n *ast.Node, symbol int) int {
	id := b.node(3, n, "using resources")
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	return id
}
func (b *irBuilder) usingCall(n *ast.Node, name string, arguments ...int) int {
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
func (b *irBuilder) usingStatement(n *ast.Node, expression int) int {
	id := b.node(18, n, ";")
	b.nodes[id-1].left = expression
	return id
}
func (b *irBuilder) usingBlock(n *ast.Node, head int) int {
	id := b.node(12, n, "{")
	b.nodes[id-1].name = b.text("scope")
	b.nodes[id-1].children = head
	return id
}
func (b *irBuilder) registerUsingBinding(n *ast.Node, row *irNode) {
	if n.Kind != ast.KindVariableDeclaration {
		return
	}
	flags := ast.GetCombinedNodeFlags(n)
	if flags&ast.NodeFlagsUsing == 0 {
		return
	}
	if flags&ast.NodeFlagsAwaitUsing == ast.NodeFlagsAwaitUsing {
		b.unsupported(n)
	}
	parent := n.Parent
	if parent == nil || parent.Parent == nil || parent.Parent.Kind != ast.KindVariableStatement {
		b.unsupported(n)
	}
	scope := parent.Parent.Parent
	if scope == nil || (scope.Kind != ast.KindBlock && scope.Kind != ast.KindSourceFile) || b.usingStack == 0 {
		b.unsupported(n)
	}
	row.left = b.usingCall(n, "__usingAdd", b.usingReference(n, b.usingStack), row.left)
	row.constant = 1
}
func (b *irBuilder) wrapUsing(n *ast.Node, stack, body int) int {
	declaration := b.node(13, n, "using resources")
	b.nodes[declaration-1].symbol = stack
	b.nodes[declaration-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[declaration-1].constant = 1
	b.nodes[declaration-1].left = b.usingCall(n, "__usingStack")
	caught := b.node(52, n, "caught")
	b.nodes[caught-1].typ = b.typ(b.c.GetAnyType())
	b.symbols = append(b.symbols, irSymbol{name: b.text("using error"), typ: b.typ(b.c.GetAnyType())})
	errorSymbol := len(b.symbols)
	binding := b.node(13, n, "let")
	b.nodes[binding-1].symbol = errorSymbol
	b.nodes[binding-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[binding-1].left = caught
	record := b.usingStatement(n, b.usingCall(n, "__usingError", b.usingReference(n, stack), b.usingReference(n, errorSymbol)))
	b.nodes[binding-1].next = record
	dispose := b.usingStatement(n, b.usingCall(n, "__usingDispose", b.usingReference(n, stack)))
	protected := b.node(51, n, "try")
	bodyBlock := b.usingBlock(n, body)
	// Module bindings stay module bindings inside the cleanup wrapper; they
	// must not become captures of an artificial nested lexical scope.
	if n.Kind == ast.KindSourceFile {
		b.nodes[bodyBlock-1].name = 0
	}
	b.nodes[protected-1].left = bodyBlock
	b.nodes[protected-1].right = b.usingBlock(n, binding)
	b.nodes[protected-1].third = b.usingBlock(n, dispose)
	b.nodes[declaration-1].next = protected
	return declaration
}

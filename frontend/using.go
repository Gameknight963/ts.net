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
		owner := enclosingAsyncFunction(n)
		if owner == nil || ast.GetCombinedModifierFlags(owner)&ast.ModifierFlagsAsync == 0 {
			b.unsupported(n)
		}
	}
	parent := n.Parent
	// Iteration binds the value before entering its per-iteration cleanup scope.
	if parent != nil && parent.Parent != nil && parent.Parent.Kind == ast.KindForOfStatement {
		row.constant = 1
		return
	}
	if parent == nil || parent.Parent == nil || b.usingStack == 0 {
		b.unsupported(n)
	}
	if b.usingFor == nil || parent.Parent != b.usingFor {
		if parent.Parent.Kind != ast.KindVariableStatement {
			b.unsupported(n)
		}
		scope := parent.Parent.Parent
		if scope == nil || (scope.Kind != ast.KindBlock && scope.Kind != ast.KindSourceFile && scope.Kind != ast.KindModuleBlock) {
			b.unsupported(n)
		}
	}
	row.left = b.builtinCall(n, b.usingAddName(flags, b.usingStack), b.usingReference(n, b.usingStack), row.left)
	row.constant = 1
}
func (b *irBuilder) wrapUsing(n *ast.Node, stack, body int) int {
	declaration := b.node(13, n, "using resources")
	b.nodes[declaration-1].symbol = stack
	b.nodes[declaration-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[declaration-1].constant = 1
	b.nodes[declaration-1].left = b.builtinCall(n, "__usingStack")
	caught := b.node(52, n, "caught")
	b.nodes[caught-1].typ = b.typ(b.c.GetAnyType())
	b.symbols = append(b.symbols, irSymbol{name: b.text("using error"), typ: b.typ(b.c.GetAnyType())})
	errorSymbol := len(b.symbols)
	binding := b.node(13, n, "let")
	b.nodes[binding-1].symbol = errorSymbol
	b.nodes[binding-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[binding-1].left = caught
	record := b.expressionStatement(n, b.builtinCall(n, "__usingError", b.usingReference(n, stack), b.usingReference(n, errorSymbol)))
	b.nodes[binding-1].next = record
	disposeName := "__usingDispose"
	if b.usingAsync[stack] {
		disposeName = "__usingDisposeAsync"
	}
	disposal := b.builtinCall(n, disposeName, b.usingReference(n, stack))
	if b.usingAsync[stack] {
		awaited := b.node(rawAsyncAwait, n, "await disposal")
		b.nodes[awaited-1].typ = b.typ(b.c.GetAnyType())
		b.nodes[awaited-1].left = disposal
		disposal = awaited
	}
	dispose := b.expressionStatement(n, disposal)
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

func (b *irBuilder) lowerUsingFor(n *ast.Node) int {
	previousStack, previousLoop := b.usingStack, b.usingFor
	b.usingStack, b.usingFor = b.newUsingStack(), n
	defer func() { b.usingStack, b.usingFor = previousStack, previousLoop }()
	body := b.lower(n)
	return b.usingBlock(n, b.wrapUsing(n, b.usingStack, body))
}

func (b *irBuilder) lowerUsingIteration(n *ast.Node, binding int) int {
	previous := b.usingStack
	b.usingStack = b.newUsingStack()
	defer func() { b.usingStack = previous }()
	if binding == 0 || b.nodes[binding-1].kind != 13 || b.nodes[binding-1].symbol == 0 {
		b.unsupported(n)
	}
	// The loop has already assigned this iteration's value to the binding.
	reference := b.usingReference(n, b.nodes[binding-1].symbol)
	add := b.expressionStatement(n, b.builtinCall(n, b.usingAddName(ast.GetCombinedNodeFlags(n.AsForInOrOfStatement().Initializer), b.usingStack), b.usingReference(n, b.usingStack), reference))
	body := b.lower(n.AsForInOrOfStatement().Statement)
	b.nodes[add-1].next = body
	return b.usingBlock(n, b.wrapUsing(n, b.usingStack, add))
}

func (b *irBuilder) usingAddName(flags ast.NodeFlags, stack int) string {
	if flags&ast.NodeFlagsAwaitUsing == ast.NodeFlagsAwaitUsing {
		if b.usingAsync == nil {
			b.usingAsync = map[int]bool{}
		}
		b.usingAsync[stack] = true
		return "__usingAddAsync"
	}
	return "__usingAdd"
}

package main

import "github.com/microsoft/typescript-go/internal/ast"

// Register all merged bodies before lowering functions that may read exports
// declared in a later body.
func (b *irBuilder) prepareEnumNamespaces(files []*ast.SourceFile) {
	b.namespaceExports = map[*ast.Symbol]*ast.Node{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindModuleDeclaration && n.Body() != nil && n.Body().Kind == ast.KindModuleBlock {
			symbol := b.c.GetSymbolAtLocation(n.Name())
			if symbol != nil && symbol.Flags&ast.SymbolFlagsRegularEnum != 0 {
				for _, statement := range n.Body().AsModuleBlock().Statements.Nodes {
					if ast.GetCombinedModifierFlags(statement)&ast.ModifierFlagsExport == 0 {
						continue
					}
					register := func(d *ast.Node) {
						if d.Name() != nil && d.Name().Kind == ast.KindIdentifier {
							b.namespaceExports[b.c.GetSymbolAtLocation(d.Name())] = n
						}
					}
					if statement.Kind == ast.KindVariableStatement {
						for _, d := range statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes {
							register(d)
						}
					} else {
						register(statement)
					}
				}
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}

func (b *irBuilder) namespaceReference(n, owner *ast.Node) int {
	id := b.node(5, n, n.Text())
	b.nodes[id-1].name = b.text(n.Text())
	b.nodes[id-1].left = b.enumOwner(n, owner)
	b.nodes[id-1].typ = b.typ(b.c.GetTypeAtLocation(n))
	return id
}

// Each namespace body gets its own closure for private declarations. Exported
// bindings live on the enum object, so writes remain visible through aliases.
func (b *irBuilder) enumNamespace(n *ast.Node) int {
	if ast.HasAmbientModifier(n) {
		return 0
	}
	symbol := b.c.GetSymbolAtLocation(n.Name())
	if symbol == nil || symbol.Flags&ast.SymbolFlagsRegularEnum == 0 || n.Body() == nil || n.Body().Kind != ast.KindModuleBlock {
		b.unsupported(n)
	}
	if b.namespaceExports == nil {
		b.namespaceExports = map[*ast.Symbol]*ast.Node{}
	}
	statements := n.Body().AsModuleBlock().Statements.Nodes
	exported := map[int]*ast.Node{}
	var register func(*ast.Node)
	register = func(d *ast.Node) {
		if d.Name() == nil || d.Name().Kind != ast.KindIdentifier {
			b.unsupported(d)
		}
		name := d.Name()
		b.namespaceExports[b.c.GetSymbolAtLocation(name)] = n
		exported[b.symbol(name)] = name
	}
	for _, statement := range statements {
		if ast.GetCombinedModifierFlags(statement)&ast.ModifierFlagsExport == 0 {
			continue
		}
		if statement.Kind == ast.KindVariableStatement {
			for _, d := range statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes {
				register(d)
			}
		} else if statement.Kind == ast.KindFunctionDeclaration || statement.Kind == ast.KindClassDeclaration {
			register(statement)
		} else if statement.Kind != ast.KindInterfaceDeclaration && statement.Kind != ast.KindTypeAliasDeclaration {
			b.unsupported(statement)
		}
	}
	block := b.enumInitialization(n)
	last := b.nodes[block-1].children
	for b.nodes[last-1].next != 0 {
		last = b.nodes[last-1].next
	}
	function, value := b.newFunction(n, "")
	first := len(b.nodes)
	body := b.node(12, n, "namespace scope")
	b.nodes[body-1].name = b.text("scope")
	b.nodes[body-1].children = b.blockStatements(statements)
	b.nodes[function-1].left = body
	end := len(b.nodes)
	for i := first; i < end; i++ {
		node := b.nodes[i]
		name := exported[node.symbol]
		if node.kind != 13 || name == nil {
			continue
		}
		if node.left == 0 {
			b.nodes[i].kind = 12
			b.nodes[i].symbol = 0
			continue
		}
		target := b.namespaceReference(name, n)
		assignment := b.node(10, name, "=")
		b.nodes[assignment-1].left = target
		b.nodes[assignment-1].right = node.left
		b.nodes[assignment-1].typ = node.typ
		b.nodes[i] = irNode{kind: 18, token: node.token, left: assignment, next: node.next}
	}
	call := b.node(6, n, "namespace")
	b.nodes[call-1].left = value
	b.nodes[call-1].typ = b.typ(b.c.GetAnyType())
	statement := b.node(18, n, ";")
	b.nodes[statement-1].left = call
	b.nodes[last-1].next = statement
	return block
}

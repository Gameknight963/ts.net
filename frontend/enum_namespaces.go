package main

import "github.com/microsoft/typescript-go/internal/ast"

func enumNamespaceBody(n *ast.Node) []*ast.Node {
	if n.Body() == nil {
		return nil
	}
	if n.Body().Kind == ast.KindModuleBlock {
		return n.Body().AsModuleBlock().Statements.Nodes
	}
	if n.Body().Kind == ast.KindModuleDeclaration {
		return []*ast.Node{n.Body()}
	}
	return nil
}

func namespaceExported(n *ast.Node) bool {
	return ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsExport != 0 || n.Parent != nil && n.Parent.Kind == ast.KindModuleDeclaration
}

func namespaceBindingNames(n *ast.Node, register func(*ast.Node)) {
	if n.Kind == ast.KindIdentifier {
		register(n)
		return
	}
	if n.Kind == ast.KindArrayBindingPattern || n.Kind == ast.KindObjectBindingPattern {
		for _, element := range n.Elements() {
			if element.Name() != nil {
				namespaceBindingNames(element.Name(), register)
			}
		}
	}
}

func (b *irBuilder) isRuntimeNamespace(n *ast.Node) bool {
	for current := n; current != nil; current = current.Parent {
		if current.Kind == ast.KindModuleDeclaration {
			symbol := b.c.GetSymbolAtLocation(current.Name())
			if symbol != nil && symbol.Flags&(ast.SymbolFlagsRegularEnum|ast.SymbolFlagsValueModule) != 0 {
				return true
			}
		}
	}
	return false
}

// Register all merged bodies before lowering functions that may read exports
// declared in a later body.
func (b *irBuilder) prepareEnumNamespaces(files []*ast.SourceFile) {
	b.namespaceExports = map[*ast.Symbol]*ast.Node{}
	b.namespaceOwners = map[*ast.Symbol]int{}
	var visit func(*ast.Node)
	// Import-equals creates a snapshot binding, rather than sharing the target's
	// symbol as ordinary ES imports do. Allocate these before any references.
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindImportEqualsDeclaration && b.importAliases[n] && n.AsImportEqualsDeclaration().ModuleReference.Kind != ast.KindExternalModuleReference {
			symbol := b.c.GetSymbolAtLocation(n.Name())
			b.symbols = append(b.symbols, irSymbol{name: b.text(n.Name().Text()), typ: b.typ(b.c.GetTypeOfSymbol(b.c.GetAliasedSymbol(symbol)))})
			b.symbolIDs[symbol] = len(b.symbols)
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindModuleDeclaration {
			if b.isRuntimeNamespace(n) {
				for _, statement := range enumNamespaceBody(n) {
					if !namespaceExported(statement) {
						continue
					}
					register := func(d *ast.Node) {
						if d.Name() != nil {
							namespaceBindingNames(d.Name(), func(name *ast.Node) { b.namespaceExports[b.c.GetSymbolAtLocation(name)] = n })
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
	value := b.enumOwner(n, owner)
	b.nodes[id-1].left = value
	b.nodes[id-1].typ = b.typ(b.c.GetTypeAtLocation(n))
	return id
}

func namespaceLocalFunction(n, owner *ast.Node, symbol *ast.Symbol) bool {
	if symbol == nil || symbol.ValueDeclaration == nil || symbol.ValueDeclaration.Kind != ast.KindFunctionDeclaration || symbol.ValueDeclaration.Parent != owner.Body() {
		return false
	}
	for current := n; current != nil; current = current.Parent {
		if current == owner.Body() {
			return true
		}
	}
	return false
}

func (b *irBuilder) enumNamespaceStatements(statements []*ast.Node, owner *ast.Node) int {
	// Local functions are hoisted, but publishing them on the namespace must
	// happen at their declaration's position in the source.
	first, last := 0, 0
	appendStatement := func(id int) {
		if id == 0 {
			return
		}
		if last == 0 {
			first = id
		} else {
			b.nodes[last-1].next = id
		}
		last = id
		for b.nodes[last-1].next != 0 {
			last = b.nodes[last-1].next
		}
	}
	for _, statement := range statements {
		if statement.Kind == ast.KindFunctionDeclaration {
			appendStatement(b.lower(statement))
		}
	}
	for _, statement := range statements {
		if statement.Kind != ast.KindFunctionDeclaration {
			appendStatement(b.lower(statement))
		} else if statement.Body() != nil && ast.GetCombinedModifierFlags(statement)&ast.ModifierFlagsExport != 0 {
			name := statement.Name()
			value := b.node(3, name, name.Text())
			b.nodes[value-1].name = b.text(name.Text())
			b.nodes[value-1].symbol = b.symbol(name)
			b.nodes[value-1].typ = b.symbols[b.nodes[value-1].symbol-1].typ
			target := b.namespaceReference(name, owner)
			assignment := b.node(10, name, "=")
			b.nodes[assignment-1].left = target
			b.nodes[assignment-1].right = value
			b.nodes[assignment-1].typ = b.nodes[value-1].typ
			export := b.node(18, statement, ";")
			b.nodes[export-1].left = assignment
			appendStatement(export)
		}
	}
	return first
}

// Each namespace body gets its own closure for private declarations. Exported
// bindings live on the namespace object, so writes remain visible through aliases.
func (b *irBuilder) enumNamespace(n *ast.Node) int {
	if ast.HasAmbientModifier(n) || !ast.IsInstantiatedModule(n, false) {
		return 0
	}
	if !b.isRuntimeNamespace(n) || n.Body() == nil || (n.Body().Kind != ast.KindModuleBlock && n.Body().Kind != ast.KindModuleDeclaration) {
		b.unsupported(n)
	}
	if b.namespaceExports == nil {
		b.namespaceExports = map[*ast.Symbol]*ast.Node{}
	}
	statements := enumNamespaceBody(n)
	exported := map[int]*ast.Node{}
	var register func(*ast.Node)
	register = func(d *ast.Node) {
		if d.Name() == nil {
			b.unsupported(d)
		}
		namespaceBindingNames(d.Name(), func(name *ast.Node) {
			b.namespaceExports[b.c.GetSymbolAtLocation(name)] = n
			exported[b.symbol(name)] = name
		})
	}
	for _, statement := range statements {
		if !namespaceExported(statement) {
			continue
		}
		if statement.Kind == ast.KindVariableStatement {
			for _, d := range statement.AsVariableStatement().DeclarationList.AsVariableDeclarationList().Declarations.Nodes {
				register(d)
			}
		} else if statement.Kind == ast.KindFunctionDeclaration || statement.Kind == ast.KindClassDeclaration || statement.Kind == ast.KindModuleDeclaration || statement.Kind == ast.KindEnumDeclaration || statement.Kind == ast.KindImportEqualsDeclaration {
			register(statement)
		} else if statement.Kind != ast.KindInterfaceDeclaration && statement.Kind != ast.KindTypeAliasDeclaration {
			b.unsupported(statement)
		}
	}
	block := b.enumInitialization(n)
	last := b.nodes[block-1].children
	for last != 0 && b.nodes[last-1].next != 0 {
		last = b.nodes[last-1].next
	}
	function, value := b.newFunction(n, "")
	first := len(b.nodes)
	// Each body captures the object it initialized. Replacing a namespace's
	// property later must not redirect closures created in its previous body.
	owner := b.enumOwner(n, n)
	ownerType := b.nodes[owner-1].typ
	b.symbols = append(b.symbols, irSymbol{name: b.text("$namespace"), typ: ownerType})
	ownerBinding := len(b.symbols)
	local := b.node(13, n, "$namespace")
	b.nodes[local-1].symbol = ownerBinding
	b.nodes[local-1].typ = ownerType
	b.nodes[local-1].left = owner
	ownerSymbol := b.c.GetSymbolAtLocation(n.Name())
	previousOwner := b.namespaceOwners[ownerSymbol]
	b.namespaceOwners[ownerSymbol] = ownerBinding
	body := b.node(12, n, "namespace scope")
	b.nodes[body-1].name = b.text("scope")
	b.nodes[body-1].children = local
	previousUsing := b.usingStack
	if b.hasUsingDeclarations(statements) {
		b.usingStack = b.newUsingStack()
	}
	statementsBody := b.enumNamespaceStatements(statements, n)
	if b.usingStack != previousUsing {
		statementsBody = b.wrapUsing(n, b.usingStack, statementsBody)
	}
	b.usingStack = previousUsing
	b.nodes[local-1].next = statementsBody
	b.nodes[function-1].left = body
	end := len(b.nodes)
	for i := first; i < end; i++ {
		node := b.nodes[i]
		name := exported[node.symbol]
		if node.kind == 37 && name != nil {
			b.nodes[i].fourth = b.namespaceReference(name, n)
			continue
		}
		// A merged namespace can supply this name for a function symbol.
		// Keep its local binding; the declaration separately publishes it.
		if node.kind != 13 || name == nil || namespaceLocalFunction(name, n, b.c.GetSymbolAtLocation(name)) {
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
	b.namespaceOwners[ownerSymbol] = previousOwner
	call := b.node(6, n, "namespace")
	b.nodes[call-1].left = value
	b.nodes[call-1].typ = b.typ(b.c.GetAnyType())
	statement := b.node(18, n, ";")
	b.nodes[statement-1].left = call
	if last == 0 {
		b.nodes[block-1].children = statement
	} else {
		b.nodes[last-1].next = statement
	}
	return block
}

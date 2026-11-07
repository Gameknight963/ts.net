package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"sort"
)

func (b *irBuilder) moduleSymbol(name string) int {
	b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: b.typ(b.c.GetAnyType())})
	return len(b.symbols)
}
func (b *irBuilder) moduleRef(n *ast.Node, symbol int) int {
	id := b.node(3, n, "module")
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	return id
}
func (b *irBuilder) moduleText(n *ast.Node, text string) int {
	id := b.node(1, n, text)
	b.nodes[id-1].typ = b.typ(b.c.GetStringType())
	return id
}
func (b *irBuilder) prepareDynamicModules(files []*ast.SourceFile) {
	if !b.moduleLoader {
		return
	}
	b.moduleRegistry = b.moduleSymbol("module registry")
	b.moduleRecords = map[*ast.SourceFile]int{}
	for _, file := range files {
		b.moduleRecords[file] = b.moduleSymbol("module record")
	}
}
func (b *irBuilder) dynamicImport(n *ast.Node) int {
	args := n.AsCallExpression().Arguments.Nodes
	if len(args) < 1 || len(args) > 2 || b.moduleRegistry == 0 {
		b.unsupported(n)
	}
	options := 0
	if len(args) == 2 {
		options = b.lower(args[1])
	} else {
		options = b.node(20, n, "undefined")
		b.nodes[options-1].typ = b.typ(b.c.GetAnyType())
	}
	return b.builtinCall(n, "__moduleImport", b.moduleRef(n, b.moduleRegistry), b.moduleText(n, sourceFileURL(b.file)), b.lower(args[0]), options)
}
func (b *irBuilder) initializeDynamicModules(files []*ast.SourceFile, entry *ast.SourceFile) {
	if !b.moduleLoader {
		return
	}
	initializers := map[*ast.SourceFile]int{}
	block := b.statements
	for _, file := range files {
		next := b.nodes[block-1].next
		b.nodes[block-1].next = 0
		function, value := b.newFunction(file.AsNode(), "")
		if b.moduleAsync[file] {
			marker := b.node(20, file.AsNode(), "async module")
			b.nodes[marker-1].typ = b.typ(b.c.GetAnyType())
			b.nodes[function-1].right = marker
			lowering := asyncExpressionLowering{b: b, source: file.AsNode(), any: b.typ(b.c.GetAnyType())}
			block = lowering.body(block, function)
		}
		b.nodes[function-1].left = block
		initializers[file] = value
		block = next
	}
	root := b.node(12, entry.AsNode(), "module registry")
	b.nodes[root-1].constant = 1
	last := 0
	appendNode := func(id int) {
		if last == 0 {
			b.nodes[root-1].children = id
		} else {
			b.nodes[last-1].next = id
		}
		last = id
	}
	variable := func(n *ast.Node, symbol, value int) int {
		id := b.node(13, n, "module")
		b.nodes[id-1].symbol = symbol
		b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
		b.nodes[id-1].left = value
		return id
	}
	appendNode(variable(entry.AsNode(), b.moduleRegistry, b.builtinCall(entry.AsNode(), "__moduleRegistry")))
	for _, file := range files {
		n := file.AsNode()
		namespace := 0
		if symbol := n.Symbol(); symbol != nil {
			namespace = b.moduleNamespaceReference(n, symbol)
		} else {
			namespace = b.node(20, n, "undefined")
			b.nodes[namespace-1].typ = b.typ(b.c.GetAnyType())
		}
		value := b.builtinCall(n, "__moduleCreate", b.moduleRef(n, b.moduleRegistry), b.moduleText(n, sourceFileURL(file)), namespace, initializers[file])
		appendNode(variable(n, b.moduleRecords[file], value))
	}
	for _, file := range files {
		n := file.AsNode()
		for _, dependency := range b.moduleDependencies[file] {
			appendNode(b.expressionStatement(n, b.builtinCall(n, "__moduleDepend", b.moduleRef(n, b.moduleRecords[file]), b.moduleRef(n, b.moduleRecords[dependency]))))
		}
		names := []string{}
		for name := range b.moduleAliases[file] {
			names = append(names, name)
		}
		sort.Strings(names)
		for _, name := range names {
			appendNode(b.expressionStatement(n, b.builtinCall(n, "__moduleAlias", b.moduleRef(n, b.moduleRegistry), b.moduleText(n, sourceFileURL(file)), b.moduleText(n, name), b.moduleRef(n, b.moduleRecords[b.moduleAliases[file][name]]))))
		}
	}
	appendNode(b.expressionStatement(entry.AsNode(), b.builtinCall(entry.AsNode(), "__moduleRun", b.moduleRef(entry.AsNode(), b.moduleRecords[entry]))))
	b.statements = root
}

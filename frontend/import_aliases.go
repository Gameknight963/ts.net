package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) importAliasValue(n *ast.Node) int {
	symbol := b.c.GetSymbolAtLocation(n)
	if symbol.Flags&ast.SymbolFlagsAlias != 0 {
		symbol = b.c.GetAliasedSymbol(symbol)
	}
	typ := b.typ(b.c.GetTypeOfSymbol(symbol))
	if n.Kind == ast.KindIdentifier {
		id := b.lower(n)
		b.nodes[id-1].typ = typ
		return id
	}
	if n.Kind != ast.KindQualifiedName {
		b.unsupported(n)
	}
	qualified := n.AsQualifiedName()
	id := b.node(5, n, qualified.Right.Text())
	b.nodes[id-1].name = b.text(qualified.Right.Text())
	left := b.importAliasValue(qualified.Left)
	b.nodes[id-1].left = left
	b.nodes[id-1].typ = typ
	return id
}

func (b *irBuilder) importEquals(n *ast.Node) int {
	if !b.importAliases[n] {
		return 0
	}
	reference := n.AsImportEqualsDeclaration().ModuleReference
	if reference.Kind == ast.KindExternalModuleReference {
		// External aliases read the compiled module's exported value or namespace.
		return 0
	}
	symbol := b.symbol(n.Name())
	id := b.node(13, n, n.Name().Text())
	b.nodes[id-1].name = b.text(n.Name().Text())
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.symbols[symbol-1].typ
	value := b.importAliasValue(reference)
	b.nodes[id-1].left = value
	marker := b.node(1, n, "var")
	b.nodes[id-1].fourth = marker
	return id
}

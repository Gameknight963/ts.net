package main

import (
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
)

func (b *irBuilder) privateSymbol(name *ast.Node) *ast.Symbol {
	if symbol := b.c.GetSymbolAtLocation(name); b.privateMembers[symbol] != 0 {
		return symbol
	}
	for owner := name.Parent; owner != nil; owner = owner.Parent {
		if owner.Kind != ast.KindClassDeclaration && owner.Kind != ast.KindClassExpression {
			continue
		}
		for _, member := range owner.Members() {
			if member.Name() != nil && member.Name().Kind == ast.KindPrivateIdentifier && member.Name().Text() == name.Text() {
				return b.c.GetSymbolAtLocation(member.Name())
			}
		}
	}
	return nil
}

func (b *irBuilder) privateReference(name *ast.Node) int {
	symbol := b.privateMembers[b.privateSymbol(name)]
	if symbol == 0 {
		b.unsupported(name)
	}
	return b.classReference(name, symbol)
}

func (b *irBuilder) privateProperty(n *ast.Node) int {
	id := b.node(5, n, n.Name().Text())
	b.nodes[id-1].name = b.text(n.Name().Text())
	left := b.lower(n.Expression())
	slot := b.privateReference(n.Name())
	marker := b.node(20, n, "private storage type")
	b.nodes[marker-1].typ = b.typ(b.c.GetTypeOfSymbol(b.privateSymbol(n.Name())))
	b.nodes[id-1].left = left
	b.nodes[id-1].fourth = slot
	b.nodes[id-1].third = marker
	b.nodes[id-1].typ = b.typ(b.c.GetTypeAtLocation(n))
	return id
}

func privateKind(n *ast.Node) int {
	switch n.Kind {
	case ast.KindMethodDeclaration:
		return 1
	case ast.KindGetAccessor:
		return 2
	case ast.KindSetAccessor:
		return 3
	}
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAccessor != 0 {
		return 4
	}
	return 0
}

func (b *irBuilder) privateInitialization(member *ast.Node, receiver int, expression int, mode int) int {
	id := b.node(57, member, "private initialization")
	b.nodes[id-1].symbol = b.privateMembers[b.c.GetSymbolAtLocation(member.Name())]
	left := b.classReference(member, receiver)
	slot := b.privateReference(member.Name())
	marker := b.node(0, member, strconv.Itoa(mode))
	b.nodes[marker-1].typ = b.typ(b.c.GetNumberType())
	b.nodes[id-1].left = left
	b.nodes[id-1].right = expression
	b.nodes[id-1].fourth = slot
	b.nodes[id-1].third = marker
	if mode != 1 && anonymousPropertyFunction(member.Initializer()) {
		b.nodes[id-1].constant = 1
	}
	return id
}

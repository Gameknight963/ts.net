package main

import "github.com/microsoft/typescript-go/internal/ast"

// Function.right is metadata reserved for async execution. Keep the marker a
// real IR node so tree walks do not interpret a boolean as an unrelated node ID.
func (b *irBuilder) markAsyncFunction(function int, n *ast.Node) bool {
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAsync == 0 {
		return false
	}
	if n.Kind == ast.KindFunctionDeclaration && n.AsFunctionDeclaration().AsteriskToken != nil {
		b.unsupported(n)
	}
	marker := b.node(20, n, "async")
	b.nodes[marker-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[function-1].right = marker
	return true
}

package main

import "github.com/microsoft/typescript-go/internal/ast"

func awaitOwner(n *ast.Node) *ast.Node {
	child := n
	for parent := n.Parent; parent != nil; parent = parent.Parent {
		// A computed name runs in the surrounding scope, not the method body.
		if ast.IsFunctionLike(parent) && child != parent.Name() {
			return parent
		}
		child = parent
	}
	return ast.GetSourceFileOfNode(n).AsNode()
}
func hasModuleAwait(n *ast.Node) bool {
	if n == nil {
		return false
	}
	if ast.IsFunctionLike(n) {
		return hasModuleAwait(n.Name())
	}
	if n.Kind == ast.KindAwaitExpression || n.Kind == ast.KindForOfStatement && n.AsForInOrOfStatement().AwaitModifier != nil || n.Kind == ast.KindVariableDeclarationList && ast.GetCombinedNodeFlags(n)&ast.NodeFlagsAwaitUsing == ast.NodeFlagsAwaitUsing {
		return true
	}
	found := false
	n.ForEachChild(func(child *ast.Node) bool {
		if hasModuleAwait(child) {
			found = true
		}
		return found
	})
	return found
}

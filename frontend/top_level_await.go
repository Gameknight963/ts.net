package main

import "github.com/microsoft/typescript-go/internal/ast"

func awaitOwner(n *ast.Node) *ast.Node {
	if owner := enclosingAsyncFunction(n); owner != nil {
		return owner
	}
	return ast.GetSourceFileOfNode(n).AsNode()
}
func hasModuleAwait(n *ast.Node) bool {
	if ast.IsFunctionLike(n) {
		return false
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

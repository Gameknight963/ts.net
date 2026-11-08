package main

import "github.com/microsoft/typescript-go/internal/ast"

// Namespace aliases can be changed by code outside the direct call site.
// Keep those programs on ordinary property reads rather than cached intrinsics.
func builtinNamespaceMayChange(n *ast.Node) bool {
	for n.Parent != nil && n.Parent.Kind == ast.KindParenthesizedExpression {
		n = n.Parent
	}
	parent := n.Parent
	if parent == nil {
		return false
	}
	switch parent.Kind {
	case ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
		if parent.Expression() != n {
			return true
		}
		for parent.Parent != nil && parent.Parent.Kind == ast.KindParenthesizedExpression {
			parent = parent.Parent
		}
		return identifierWrite(parent) || parent.Parent != nil && parent.Parent.Kind == ast.KindDeleteExpression
	case ast.KindCallExpression, ast.KindNewExpression:
		return parent.Expression() != n
	case ast.KindVariableDeclaration:
		return parent.Initializer() != n || parent.Name().Kind != ast.KindObjectBindingPattern
	}
	return true
}
func (b *irBuilder) prepareBuiltinMethodWrites(files []*ast.SourceFile) {
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindIdentifier && (n.Text() == "Number" || n.Text() == "Object") && b.isBuiltinIdentifier(n) && builtinNamespaceMayChange(n) {
			if n.Text() == "Number" {
				b.mutableNumberMethods = true
			} else {
				b.mutableObjectEnumeration = true
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}

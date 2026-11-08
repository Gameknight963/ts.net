package main

import "github.com/microsoft/typescript-go/internal/ast"

func builtinPropertyMayBeWritten(n *ast.Node) bool {
	for n.Parent != nil {
		if identifierWrite(n) {
			return true
		}
		parent := n.Parent
		switch parent.Kind {
		case ast.KindParenthesizedExpression, ast.KindArrayLiteralExpression, ast.KindObjectLiteralExpression, ast.KindSpreadElement, ast.KindSpreadAssignment:
			n = parent
		case ast.KindPropertyAssignment:
			if parent.Initializer() != n {
				return false
			}
			n = parent
		case ast.KindForOfStatement, ast.KindForInStatement:
			return parent.AsForInOrOfStatement().Initializer == n
		case ast.KindDeleteExpression:
			return parent.Expression() == n
		default:
			return false
		}
	}
	return false
}

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
		return builtinPropertyMayBeWritten(parent)
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
		if n.Kind == ast.KindIdentifier && (n.Text() == "Number" || n.Text() == "Object" || n.Text() == "console" || n.Text() == "performance") && b.isBuiltinIdentifier(n) && builtinNamespaceMayChange(n) {
			switch n.Text() {
			case "Number":
				b.mutableNumberMethods = true
			case "Object":
				b.mutableObjectMethods = true
			case "console":
				b.mutableConsoleLog = true
			case "performance":
				b.mutablePerformanceNow = true
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}

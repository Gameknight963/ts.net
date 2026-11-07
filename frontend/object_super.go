package main

import "github.com/microsoft/typescript-go/internal/ast"

func objectMethodHasSuper(n *ast.Node) bool {
	if n == nil || n.Parent == nil || n.Parent.Kind != ast.KindObjectLiteralExpression || (n.Kind != ast.KindMethodDeclaration && n.Kind != ast.KindGetAccessor && n.Kind != ast.KindSetAccessor) {
		return false
	}
	var visit func(*ast.Node) bool
	visit = func(node *ast.Node) bool {
		if node == nil {
			return false
		}
		if node.Kind == ast.KindSuperKeyword {
			return true
		}
		if ast.IsClassLike(node) {
			if clauses := node.ClassLikeData().HeritageClauses; clauses != nil {
				for _, clause := range clauses.Nodes {
					if visit(clause) {
						return true
					}
				}
			}
			for _, member := range node.Members() {
				if name := member.Name(); name != nil && name.Kind == ast.KindComputedPropertyName && visit(name.Expression()) {
					return true
				}
			}
			return false
		}
		if ast.IsFunctionLike(node) && node.Kind != ast.KindArrowFunction {
			return visit(node.Name())
		}
		return node.ForEachChild(visit)
	}
	for _, parameter := range n.Parameters() {
		if visit(parameter) {
			return true
		}
	}
	return visit(n.Body())
}

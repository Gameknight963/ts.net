package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// A derived constructor can define fields on an object returned by its base.
// Those fields can even change type, so matching literals need dynamic storage.
func (b *irBuilder) prepareConstructorReturns(constructor *ast.Node) {
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if ast.IsFunctionLike(n) || ast.IsClassLike(n) {
			return
		}
		if n.Kind == ast.KindReturnStatement && n.Expression() != nil {
			value := n.Expression()
			for value.Kind == ast.KindParenthesizedExpression || value.Kind == ast.KindAsExpression || value.Kind == ast.KindTypeAssertionExpression || value.Kind == ast.KindNonNullExpression || value.Kind == ast.KindSatisfiesExpression {
				value = value.Expression()
			}
			b.prepareConstructorReturnType(b.c.GetTypeAtLocation(value))
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	if constructor.Body() != nil {
		visit(constructor.Body())
	}
}

func (b *irBuilder) prepareConstructorReturnType(t *checker.Type) {
	if t.Flags()&checker.TypeFlagsUnion != 0 {
		for _, part := range t.Types() {
			b.prepareConstructorReturnType(part)
		}
		return
	}
	if t.Flags()&(checker.TypeFlagsAny|checker.TypeFlagsUnknown|checker.TypeFlagsNonPrimitive) != 0 {
		b.deleteAnyName = true
	} else if t.Flags()&(checker.TypeFlagsObject|checker.TypeFlagsIntersection|checker.TypeFlagsTypeParameter) != 0 {
		properties := b.c.GetPropertiesOfType(t)
		if len(properties) == 0 {
			b.deleteAnyName = true
		}
		for _, property := range properties {
			b.deletedNames[ast.SymbolName(property)] = true
		}
	}
}

func (b *irBuilder) prepareFunctionBaseReturns(n *ast.Node, seen map[*ast.Node]bool) {
	if n == nil || seen[n] {
		return
	}
	seen[n] = true
	switch n.Kind {
	case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
		b.prepareFunctionBaseReturns(n.Expression(), seen)
	case ast.KindFunctionDeclaration, ast.KindFunctionExpression:
		b.prepareConstructorReturns(n)
	case ast.KindVariableDeclaration, ast.KindPropertyAssignment, ast.KindPropertyDeclaration:
		b.prepareFunctionBaseReturns(n.Initializer(), seen)
	case ast.KindIdentifier, ast.KindPropertyAccessExpression, ast.KindElementAccessExpression:
		symbol := b.c.GetSymbolAtLocation(n)
		if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
			symbol = b.c.GetAliasedSymbol(symbol)
		}
		if symbol != nil {
			b.prepareFunctionBaseReturns(symbol.ValueDeclaration, seen)
		}
	}
}

func (b *irBuilder) prepareClassBaseReturns(n *ast.Node) {
	if n.ClassLikeData().HeritageClauses == nil {
		return
	}
	for _, clause := range n.ClassLikeData().HeritageClauses.Nodes {
		if clause.AsHeritageClause().Token == ast.KindExtendsKeyword {
			b.prepareFunctionBaseReturns(clause.AsHeritageClause().Types.Nodes[0].Expression(), map[*ast.Node]bool{})
		}
	}
}

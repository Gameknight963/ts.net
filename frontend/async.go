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

// A terminal await has no live CLR evaluation stack to retain. Other await
// positions still need continuation lowering and must not execute synchronously.
func (b *irBuilder) asyncReturnExpression(expression *ast.Node) int {
	if expression == nil {
		return b.lower(expression)
	}
	await := expression
	for await.Kind == ast.KindParenthesizedExpression {
		await = await.Expression()
	}
	if await.Kind != ast.KindAwaitExpression {
		return b.lower(expression)
	}
	foundFunction := false
	for parent := await.Parent; parent != nil; parent = parent.Parent {
		if ast.IsFunctionLike(parent) {
			if ast.GetCombinedModifierFlags(parent)&ast.ModifierFlagsAsync == 0 {
				b.unsupported(await)
			}
			foundFunction = true
			break
		}
		if parent.Kind == ast.KindTryStatement {
			b.unsupported(await)
		}
	}
	if !foundFunction {
		b.unsupported(await)
	}
	result := b.node(70, await, "return await")
	b.nodes[result-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[result-1].left = b.lower(await.Expression())
	return result
}

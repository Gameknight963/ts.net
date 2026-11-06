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

// Await expressions lower to synchronous continuation functions. The runtime
// schedules them through intrinsic Promise reactions, retaining one outer Promise.
func (b *irBuilder) asyncReturnExpression(expression *ast.Node) int {
	if expression == nil || !expressionHasAwait(expression) {
		return b.lower(expression)
	}
	owner := enclosingAsyncFunction(expression)
	if owner == nil || ast.GetCombinedModifierFlags(owner)&ast.ModifierFlagsAsync == 0 {
		b.unsupported(expression)
	}
	for parent := expression.Parent; parent != owner; parent = parent.Parent {
		if parent.Kind == ast.KindTryStatement {
			b.unsupported(expression)
		}
	}
	await := expression
	for await.Kind == ast.KindParenthesizedExpression {
		await = await.Expression()
	}
	// Keep the direct return-await path free of a redundant continuation frame.
	if await.Kind == ast.KindAwaitExpression && !expressionHasAwait(await.Expression()) {
		result := b.node(70, await, "return await")
		b.nodes[result-1].typ = b.typ(b.c.GetAnyType())
		b.nodes[result-1].left = b.lower(await.Expression())
		return result
	}
	previous := b.awaitReturnOwner
	b.awaitReturnOwner = owner
	value := b.lower(expression)
	b.awaitReturnOwner = previous
	lowering := asyncExpressionLowering{b: b, source: expression, any: b.typ(b.c.GetAnyType())}
	function := lowering.function(false, func(_ int) int { return lowering.expression(value, lowering.returned) })
	call := lowering.node(6, "await expression")
	b.nodes[call-1].left = function
	return call
}

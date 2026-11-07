package main

import "github.com/microsoft/typescript-go/internal/ast"

const rawGeneratorYield = -2

func generatorFunction(n *ast.Node) bool {
	switch n.Kind {
	case ast.KindFunctionDeclaration:
		return n.AsFunctionDeclaration().AsteriskToken != nil
	case ast.KindFunctionExpression:
		return n.AsFunctionExpression().AsteriskToken != nil
	case ast.KindMethodDeclaration:
		return n.AsMethodDeclaration().AsteriskToken != nil
	}
	return false
}

func (b *irBuilder) generatorBody(source *ast.Node, body int) int {
	asynchronous := ast.GetCombinedModifierFlags(b.generatorOwner)&ast.ModifierFlagsAsync != 0
	lowering := asyncExpressionLowering{b: b, source: source, any: b.typ(b.c.GetAnyType()), asyncGenerator: asynchronous}
	start := lowering.function(false, func(_ int) int { return body })
	function := b.nodes[start-1].left
	b.nodes[function-1].left = lowering.body(body, function)
	// Parameters initialize on the call; the body starts on the first next().
	name := "__generatorCreate"
	if asynchronous {
		name = "__asyncGeneratorCreate"
	}
	return lowering.returned(b.builtinCall(source, name, start))
}

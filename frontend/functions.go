package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

func (b *irBuilder) functionName(n *ast.Node) string {
	name := b.inferredExpressionName(n)
	if n.Name() != nil {
		if n.Kind == ast.KindMethodDeclaration || n.Kind == ast.KindGetAccessor || n.Kind == ast.KindSetAccessor {
			name, _ = b.literalPropertyName(n.Name())
		} else {
			name = n.Name().Text()
		}
	}
	return name
}

func (b *irBuilder) functionDebugName(n *ast.Node) string {
	name := b.functionName(n)
	if n.Kind == ast.KindConstructor {
		name = "constructor"
	}
	if n.Kind == ast.KindGetAccessor {
		name = "get_" + name
	}
	if n.Kind == ast.KindSetAccessor {
		name = "set_" + name
	}
	if owner := n.Parent; owner != nil && (owner.Kind == ast.KindClassDeclaration || owner.Kind == ast.KindClassExpression) && owner.Name() != nil {
		name = owner.Name().Text() + "_" + name
	}
	return name
}

func (b *irBuilder) functionValue(n *ast.Node) int {
	name := b.functionName(n)
	value := b.node(38, n, "function")
	typ := b.typ(b.c.GetAnyType())
	if n.Kind != ast.KindConstructor {
		typ = b.functionResultType(n)
	}
	if n.Type() == nil && !b.dynamicNumericReturn(n, map[*ast.Node]bool{}) && n.Kind != ast.KindFunctionDeclaration && n.Kind != ast.KindConstructor && ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAsync == 0 {
		if context := b.c.GetContextualType(n, checker.ContextFlagsNone); context != nil {
			if signatures := b.c.GetSignaturesOfType(context, checker.SignatureKindCall); len(signatures) == 1 {
				// A contextual unknown or void result need not erase a known scalar.
				// Function adapters still box it when callers need an object result.
				if kind := b.types[typ-1].kind; kind != 0 && kind != 2 {
					typ = b.typ(b.c.GetReturnTypeOfSignature(signatures[0]))
				}
			}
		}
	}
	symbol := 0
	if n.Name() != nil && n.Kind != ast.KindFunctionDeclaration && n.Kind != ast.KindMethodDeclaration && n.Kind != ast.KindGetAccessor && n.Kind != ast.KindSetAccessor {
		symbol = b.symbol(n.Name())
	} else {
		b.symbols = append(b.symbols, irSymbol{name: b.text(name), typ: typ})
		symbol = len(b.symbols)
	}
	function := b.node(19, n, name)
	b.symbols[symbol-1].function = function
	b.symbols[symbol-1].typ = typ
	if n.Name() != nil && n.Kind == ast.KindFunctionExpression {
		// A named expression's self value belongs to each created instance.
		b.nodes[function-1].constant = 1
	}
	suspending := b.markSuspendingFunction(function, n)
	if !suspending && (n.Kind == ast.KindFunctionDeclaration || n.Kind == ast.KindFunctionExpression) {
		b.markFunctionPrototype(function, n)
	}
	b.nodes[function-1].name = b.text(b.functionDebugName(n))
	b.nodes[function-1].symbol = symbol
	b.nodes[function-1].typ = typ
	b.nodes[function-1].next = b.functions
	b.functions = function
	// Labels and jumps cannot cross a function boundary.
	controls, labels := b.controls, b.labels
	b.controls, b.labels = nil, map[string]irLabel{}
	parameters, body := b.functionParts(n, function)
	b.controls, b.labels = controls, labels
	b.nodes[function-1].children = parameters
	b.nodes[function-1].left = body
	b.nodes[value-1].symbol = symbol
	b.nodes[value-1].left = function
	b.nodes[value-1].typ = b.typ(b.c.GetAnyType())
	if n.Kind == ast.KindFunctionExpression && n.Name() != nil {
		return b.functionIdentity(n, value, function, symbol)
	}
	return value
}

// Block declarations initialize before any statements in that block execute.
func (b *irBuilder) blockStatements(statements []*ast.Node) int {
	ordered := make([]*ast.Node, 0, len(statements))
	for _, statement := range statements {
		if statement.Kind == ast.KindFunctionDeclaration && statement.Body() != nil {
			ordered = append(ordered, statement)
		}
	}
	for _, statement := range statements {
		if statement.Kind != ast.KindFunctionDeclaration {
			ordered = append(ordered, statement)
		}
	}
	if b.hasUsingDeclarations(ordered) {
		previous := b.usingStack
		b.usingStack = b.newUsingStack()
		body := b.list(ordered)
		result := b.wrapUsing(ordered[0], b.usingStack, body)
		b.usingStack = previous
		return result
	}
	return b.list(ordered)
}

func (b *irBuilder) localFunction(n *ast.Node) int {
	if n.Body() == nil {
		return 0
	}
	if n.Parent == nil || (n.Parent.Kind != ast.KindModuleBlock && n.Parent.Kind != ast.KindBlock && n.Parent.Kind != ast.KindCaseClause && n.Parent.Kind != ast.KindDefaultClause) || n.Name() == nil {
		b.unsupported(n)
	}
	value := b.functionValue(n)
	binding := b.node(13, n, n.Name().Text())
	symbol := b.symbol(n.Name())
	typ := b.typ(b.c.GetTypeAtLocation(n.Name()))
	b.nodes[binding-1].symbol = symbol
	b.nodes[binding-1].typ = typ
	b.nodes[binding-1].left = value
	b.symbols[symbol-1].typ = typ
	return binding
}

func nonFunctionStatements(statements []*ast.Node) []*ast.Node {
	result := make([]*ast.Node, 0, len(statements))
	for _, statement := range statements {
		if statement.Kind != ast.KindFunctionDeclaration {
			result = append(result, statement)
		}
	}
	return result
}

// Keep direct self calls on their existing path. A self value instead needs an
// instance-local binding shared by the function and any nested closures.
func (b *irBuilder) functionIdentity(n *ast.Node, value, function, self int) int {
	direct := map[int]bool{}
	for i := function; i <= len(b.nodes); i++ {
		if b.nodes[i-1].kind == 6 {
			direct[b.nodes[i-1].left] = true
		}
	}
	needed := false
	for i := function; i <= len(b.nodes); i++ {
		node := b.nodes[i-1]
		if node.kind == 3 && node.symbol == self && (!direct[i] || b.nodes[function-1].right != 0 || b.nodes[function-1].fourth != 0 && b.nodes[b.nodes[function-1].fourth-1].name != 0) {
			needed = true
			break
		}
	}
	if !needed {
		return value
	}
	typ := b.typ(b.c.GetAnyType())
	original := b.symbols[self-1]
	b.symbols = append(b.symbols, irSymbol{name: original.name, typ: original.typ, function: function})
	implementation := len(b.symbols)
	b.nodes[function-1].symbol = implementation
	b.nodes[function-1].constant = 0
	b.nodes[value-1].symbol = implementation
	b.symbols[self-1].function = 0
	b.symbols[self-1].typ = typ
	binding := b.node(13, n, n.Name().Text())
	b.nodes[binding-1].name = original.name
	b.nodes[binding-1].typ = typ
	b.nodes[binding-1].symbol = self
	b.nodes[binding-1].left = value
	reference := b.node(3, n, n.Name().Text())
	b.nodes[reference-1].name = original.name
	b.nodes[reference-1].typ = typ
	b.nodes[reference-1].symbol = self
	result := b.node(14, n, "return")
	b.nodes[result-1].left = reference
	b.nodes[binding-1].next = result
	block := b.node(12, n, "{")
	b.nodes[block-1].name = b.text("scope")
	b.nodes[block-1].children = binding
	factory := b.node(19, n, "function")
	b.symbols = append(b.symbols, irSymbol{name: b.text(""), typ: typ, function: factory})
	factorySymbol := len(b.symbols)
	b.nodes[factory-1].symbol = factorySymbol
	b.nodes[factory-1].typ = typ
	b.nodes[factory-1].left = block
	b.nodes[factory-1].next = b.functions
	b.functions = factory
	factoryValue := b.node(38, n, "function")
	b.nodes[factoryValue-1].symbol = factorySymbol
	b.nodes[factoryValue-1].typ = typ
	b.nodes[factoryValue-1].left = factory
	call := b.node(6, n, "call")
	b.nodes[call-1].left = factoryValue
	b.nodes[call-1].typ = typ
	return call
}

// Ordinary functions receive their own this binding. Arrows capture the enclosing
// binding through the same closure machinery used for other parameters.
func (b *irBuilder) functionParts(n *ast.Node, function int) (int, int) {
	previousGenerator := b.generatorOwner
	b.generatorOwner = nil
	generator := generatorFunction(n)
	if generator {
		b.generatorOwner = n
	}
	defer func() { b.generatorOwner = previousGenerator }()
	arrow := n.Kind == ast.KindArrowFunction
	previous, scope := b.thisSymbol, b.thisScope
	previousNewTarget := b.newTargetUsed
	previousArguments := b.argumentsUsed
	previousSuper, previousStatic := b.superSymbol, b.superStatic
	if !arrow && (n.Parent == nil || !ast.IsClassLike(n.Parent)) {
		b.superSymbol = 0
		if objectMethodHasSuper(n) {
			b.superSymbol = -1
		}
	}
	if !arrow {
		b.thisSymbol = 0
		b.thisScope = true
		b.newTargetUsed = false
		b.argumentsUsed = false
	}
	parameters := b.list(n.Parameters())
	body := 0
	if n.Body().Kind == ast.KindBlock {
		previousAwait := b.awaitReturnOwner
		async := ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAsync != 0
		if async {
			b.awaitReturnOwner = n
		}
		body = b.lower(n.Body())
		b.awaitReturnOwner = previousAwait
		if generator {
			body = b.generatorBody(n.Body(), body)
		} else if async {
			lowering := asyncExpressionLowering{b: b, source: n.Body(), any: b.typ(b.c.GetAnyType())}
			if lowering.hasAwait(body) {
				body = lowering.body(body, function)
			}
		}
	} else {
		body = b.node(14, n.Body(), "return")
		expression := b.asyncReturnExpression(n.Body())
		b.nodes[body-1].left = expression
	}
	if !arrow {
		if b.thisSymbol != 0 {
			parameter := b.node(13, n, "this")
			b.nodes[parameter-1].symbol = b.thisSymbol
			b.nodes[parameter-1].typ = b.typ(b.c.GetAnyType())
			if b.newTargetUsed {
				b.nodes[parameter-1].constant = 1
			}
			if b.argumentsUsed {
				b.nodes[parameter-1].name = b.text("arguments receiver")
			}
			b.nodes[parameter-1].next = parameters
			parameters = parameter
			b.nodes[function-1].fourth = parameter
		}
		b.thisSymbol = previous
		b.thisScope = scope
		b.newTargetUsed = previousNewTarget
		b.argumentsUsed = previousArguments
		b.superSymbol, b.superStatic = previousSuper, previousStatic
	}
	return parameters, body
}

func (b *irBuilder) markFunctionPrototype(function int, n *ast.Node) {
	marker := b.node(20, n, "function prototype")
	b.nodes[marker-1].typ = b.typ(b.c.GetUndefinedType())
	b.nodes[function-1].third = marker
}

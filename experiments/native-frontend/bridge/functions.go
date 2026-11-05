package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

func (b *irBuilder) functionValue(n *ast.Node) int {
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAsync != 0 {
		b.unsupported(n)
	}
	if n.Kind == ast.KindFunctionExpression && n.AsFunctionExpression().AsteriskToken != nil {
		b.unsupported(n)
	}
	name := ""
	parent := n.Parent
	for parent != nil && (parent.Kind == ast.KindParenthesizedExpression || parent.Kind == ast.KindAsExpression || parent.Kind == ast.KindTypeAssertionExpression || parent.Kind == ast.KindSatisfiesExpression) {
		parent = parent.Parent
	}
	if parent != nil {
		switch parent.Kind {
		case ast.KindVariableDeclaration, ast.KindPropertyAssignment:
			if property, known := b.literalPropertyName(parent.Name()); known {
				name = property
			}
		case ast.KindExportAssignment:
			name = "default"
		}
	}
	if n.Name() != nil {
		name = n.Name().Text()
	}
	value := b.node(38, n, "function")
	typ := b.typ(b.c.GetReturnTypeOfSignature(b.c.GetSignatureFromDeclaration(n)))
	if n.Type() == nil && n.Kind != ast.KindFunctionDeclaration {
		if context := b.c.GetContextualType(n, checker.ContextFlagsNone); context != nil {
			if signatures := b.c.GetSignaturesOfType(context, checker.SignatureKindCall); len(signatures) == 1 {
				typ = b.typ(b.c.GetReturnTypeOfSignature(signatures[0]))
			}
		}
	}
	symbol := 0
	if n.Name() != nil && n.Kind != ast.KindFunctionDeclaration {
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
	b.nodes[function-1].name = b.text(name)
	b.nodes[function-1].symbol = symbol
	b.nodes[function-1].typ = typ
	b.nodes[function-1].next = b.functions
	b.functions = function
	// Labels and jumps cannot cross a function boundary.
	controls, labels := b.controls, b.labels
	b.controls, b.labels = nil, map[string]irLabel{}
	parameters := b.list(n.Parameters())
	body := 0
	if n.Body().Kind == ast.KindBlock {
		body = b.lower(n.Body())
	} else {
		body = b.node(14, n.Body(), "return")
		expression := b.lower(n.Body())
		b.nodes[body-1].left = expression
	}
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
	return b.list(ordered)
}

func (b *irBuilder) localFunction(n *ast.Node) int {
	if n.Body() == nil {
		return 0
	}
	if n.Parent == nil || (n.Parent.Kind != ast.KindBlock && n.Parent.Kind != ast.KindCaseClause && n.Parent.Kind != ast.KindDefaultClause) || n.Name() == nil || n.AsFunctionDeclaration().AsteriskToken != nil {
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
		if node.kind == 3 && node.symbol == self && !direct[i] {
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

package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) functionValue(n *ast.Node) int {
	if ast.GetCombinedModifierFlags(n)&ast.ModifierFlagsAsync != 0 {
		b.unsupported(n)
	}
	if n.Kind == ast.KindFunctionExpression && n.AsFunctionExpression().AsteriskToken != nil {
		b.unsupported(n)
	}
	name := ""
	parent := n.Parent
	for parent != nil && parent.Kind == ast.KindParenthesizedExpression {
		parent = parent.Parent
	}
	if parent != nil {
		switch parent.Kind {
		case ast.KindVariableDeclaration, ast.KindPropertyAssignment:
			if parent.Name().Kind == ast.KindIdentifier || parent.Name().Kind == ast.KindStringLiteral {
				name = parent.Name().Text()
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

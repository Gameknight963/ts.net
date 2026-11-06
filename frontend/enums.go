package main

import (
	"math"
	"strconv"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/evaluator"
	"github.com/microsoft/typescript-go/internal/jsnum"
)

// Resolver queries take their own checker lock. Snapshot enum evaluations before
// holding the exclusive checker lock used while exporting the checked IR.
func (b *irBuilder) prepareEnums(files []*ast.SourceFile) {
	b.enumValues = map[*ast.Node]evaluator.Result{}
	b.enumDeclared = map[*ast.Symbol]bool{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindEnumDeclaration {
			for _, member := range n.AsEnumDeclaration().Members.Nodes {
				b.enumValues[member] = b.c.GetEmitResolver().GetEnumMemberValue(member)
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}
func (b *irBuilder) enumLiteral(n *ast.Node, value any) int {
	switch v := value.(type) {
	case jsnum.Number:
		number := float64(v)
		if number == 0 {
			number = 0
		}
		// TypeScript emits a constant -0 enum value as 0.
		negative := number < 0
		if negative {
			number = -number
		}
		literal := strconv.FormatFloat(number, 'g', -1, 64)
		if math.IsInf(number, 1) {
			literal = "Infinity"
		}
		id := b.node(0, n, literal)
		b.nodes[id-1].typ = b.typ(b.c.GetNumberType())
		if negative {
			unary := b.node(8, n, "-")
			b.nodes[unary-1].left = id
			b.nodes[unary-1].typ = b.nodes[id-1].typ
			return unary
		}
		return id
	case string:
		id := b.node(1, n, v)
		b.nodes[id-1].typ = b.typ(b.c.GetStringType())
		return id
	}
	return 0
}
func (b *irBuilder) enumOwner(n *ast.Node, declaration *ast.Node) int {
	name := declaration.Name()
	symbol := b.symbol(name)
	id := b.node(3, n, name.Text())
	b.nodes[id-1].name = b.text(name.Text())
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.symbols[symbol-1].typ
	return id
}
func (b *irBuilder) enumMemberReference(n *ast.Node, symbol *ast.Symbol) int {
	member := symbol.ValueDeclaration
	if member == nil || member.Parent == nil || member.Parent.Kind != ast.KindEnumDeclaration {
		return 0
	}
	if ast.IsEnumConst(member.Parent) {
		return b.enumLiteral(n, b.c.GetConstantValue(member))
	}
	id := b.node(5, n, ast.SymbolName(symbol))
	b.nodes[id-1].name = b.text(ast.SymbolName(symbol))
	owner := b.enumOwner(n, member.Parent)
	b.nodes[id-1].left = owner
	b.nodes[id-1].typ = b.typ(b.c.GetTypeOfSymbol(symbol))
	if ast.SymbolName(symbol) == "__proto__" {
		b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	}
	return id
}
func (b *irBuilder) enumInitialization(n *ast.Node) int {
	if ast.IsEnumConst(n) || ast.HasAmbientModifier(n) {
		return 0
	}
	symbol := b.c.GetSymbolAtLocation(n.Name())
	binding := b.symbol(n.Name())
	typ := b.symbols[binding-1].typ
	if b.types[typ-1].kind != 6 {
		b.unsupported(n)
	}
	block := b.node(12, n, "enum")
	last := 0
	appendStatement := func(statement int) {
		if last == 0 {
			b.nodes[block-1].children = statement
		} else {
			b.nodes[last-1].next = statement
		}
		last = statement
	}
	if !b.enumDeclared[symbol] {
		b.enumDeclared[symbol] = true
		variable := b.node(13, n, n.Name().Text())
		b.nodes[variable-1].name = b.text(n.Name().Text())
		b.nodes[variable-1].symbol = binding
		b.nodes[variable-1].typ = typ
		empty := b.node(20, n, "undefined")
		b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
		b.nodes[empty-1].constant = 1
		b.nodes[variable-1].left = empty
		if n.Parent.Kind == ast.KindSourceFile {
			marker := b.node(1, n, "var")
			b.nodes[variable-1].fourth = marker
		}
		appendStatement(variable)
	}
	// E || (E = {...undefined}) preserves identity across merged declarations.
	// Dynamic storage also preserves missing members and prototype assignments.
	empty := b.node(20, n, "undefined")
	b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
	spread := b.node(35, n, "...")
	b.nodes[spread-1].left = empty
	b.nodes[spread-1].typ = b.typ(b.c.GetAnyType())
	object := b.node(4, n, "{")
	b.nodes[object-1].typ = typ
	// __proto__ reads can return a prototype instead of the declared scalar.
	// Other enums retain their ordinary typed field accessors.
	for field := b.types[typ-1].fields; field != 0; field = b.fields[field-1].next {
		if b.texts[b.fields[field-1].name-1] == "__proto__" {
			b.nodes[object-1].typ = b.typ(b.c.GetAnyType())
			break
		}
	}
	b.nodes[object-1].constant = 1
	b.nodes[object-1].children = spread
	assignment := b.node(10, n, "=")
	owner := b.enumOwner(n, n)
	b.nodes[assignment-1].left = owner
	b.nodes[assignment-1].right = object
	b.nodes[assignment-1].typ = typ
	reuse := b.node(7, n, "||")
	owner = b.enumOwner(n, n)
	b.nodes[reuse-1].left = owner
	b.nodes[reuse-1].right = assignment
	b.nodes[reuse-1].typ = typ
	statement := b.node(18, n, ";")
	b.nodes[statement-1].left = reuse
	appendStatement(statement)
	return block
}

func (b *irBuilder) enumDeclaration(n *ast.Node) int {
	if ast.IsEnumConst(n) || ast.HasAmbientModifier(n) {
		return 0
	}
	block := b.enumInitialization(n)
	last := b.nodes[block-1].children
	for b.nodes[last-1].next != 0 {
		last = b.nodes[last-1].next
	}
	appendStatement := func(statement int) { b.nodes[last-1].next = statement; last = statement }
	for _, member := range n.AsEnumDeclaration().Members.Nodes {
		name, known := b.literalPropertyName(member.Name())
		if !known {
			b.unsupported(member)
		}
		result := b.enumValues[member]
		value := b.enumLiteral(member, result.Value)
		if value == 0 {
			value = b.lower(member.Initializer())
		}
		if value == 0 {
			value = b.node(20, member, "undefined")
			b.nodes[value-1].typ = b.typ(b.c.GetUndefinedType())
		}
		target := b.node(5, member, name)
		b.nodes[target-1].name = b.text(name)
		owner := b.enumOwner(member, n)
		b.nodes[target-1].left = owner
		memberSymbol := b.c.GetSymbolAtLocation(member.Name())
		b.nodes[target-1].typ = b.typ(b.c.GetTypeOfSymbol(memberSymbol))
		if name == "__proto__" {
			b.nodes[target-1].typ = b.typ(b.c.GetAnyType())
		}
		assign := b.node(10, member, "=")
		b.nodes[assign-1].left = target
		b.nodes[assign-1].right = value
		b.nodes[assign-1].typ = b.nodes[value-1].typ
		_, text := result.Value.(string)
		if !text && !result.IsSyntacticallyString {
			index := b.node(33, member, "[]")
			owner := b.enumOwner(member, n)
			b.nodes[index-1].left = owner
			b.nodes[index-1].right = assign
			b.nodes[index-1].typ = b.typ(b.c.GetStringType())
			literal := b.node(1, member, name)
			b.nodes[literal-1].typ = b.typ(b.c.GetStringType())
			reverse := b.node(10, member, "=")
			b.nodes[reverse-1].left = index
			b.nodes[reverse-1].right = literal
			b.nodes[reverse-1].typ = b.nodes[literal-1].typ
			assign = reverse
		}
		statement := b.node(18, member, ";")
		b.nodes[statement-1].left = assign
		appendStatement(statement)
	}
	return block
}

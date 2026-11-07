package main

import (
	"slices"
	"unicode/utf16"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/stringutil"
)

// ECMAScript namespace keys sort by UTF-16 units, including lone surrogates.
func moduleExportUnits(name string) []uint16 {
	units := []uint16{}
	for len(name) != 0 {
		code, size := stringutil.DecodeJSStringRune(name)
		name = name[size:]
		if code > 0xffff {
			high, low := utf16.EncodeRune(code)
			units = append(units, uint16(high), uint16(low))
		} else {
			units = append(units, uint16(code))
		}
	}
	return units
}

func (b *irBuilder) moduleNamespaceReference(n *ast.Node, module *ast.Symbol) int {
	if b.moduleNamespaces == nil {
		b.moduleNamespaces = map[*ast.Node]int{}
	}
	symbol := b.moduleNamespaces[module.Declarations[0]]
	if symbol == 0 {
		b.symbols = append(b.symbols, irSymbol{name: b.text("module namespace"), typ: b.typ(b.c.GetAnyType())})
		symbol = len(b.symbols)
		b.moduleNamespaces[module.Declarations[0]] = symbol
		b.moduleNamespaceOrder = append(b.moduleNamespaceOrder, module)
	}
	id := b.node(3, n, "module namespace")
	b.nodes[id-1].symbol = symbol
	b.nodes[id-1].typ = b.typ(b.c.GetAnyType())
	return id
}

func (b *irBuilder) initializeModuleNamespaces() {
	if len(b.moduleNamespaceOrder) == 0 {
		return
	}
	block := b.node(12, b.file.AsNode(), "module namespaces")
	b.nodes[block-1].constant = 1
	last := 0
	originalFile := b.file
	defer func() { b.file = originalFile }()
	for index := 0; index < len(b.moduleNamespaceOrder); index++ {
		module := b.moduleNamespaceOrder[index]
		n := module.ValueDeclaration
		if n == nil {
			n = module.Declarations[0]
		}
		b.file = ast.GetSourceFileOfNode(n)
		object := b.node(4, n, "module namespace")
		b.nodes[object-1].typ = b.typ(b.c.GetAnyType())
		marker := b.node(20, n, "module namespace")
		b.nodes[marker-1].typ = b.typ(b.c.GetUndefinedType())
		b.nodes[object-1].fourth = marker
		previous := 0
		exports := b.c.GetExportsOfModule(module)
		if export := b.jsonExports[b.file]; export != nil {
			exports = []*ast.Symbol{export}
		}
		keys := map[*ast.Symbol][]uint16{}
		for _, export := range exports {
			keys[export] = moduleExportUnits(ast.SymbolName(export))
		}
		slices.SortFunc(exports, func(a, b *ast.Symbol) int { return slices.Compare(keys[a], keys[b]) })
		for _, export := range exports {
			target := export
			if target.Flags&ast.SymbolFlagsAlias != 0 {
				target = b.c.GetAliasedSymbol(target)
			}
			if target.Flags&ast.SymbolFlagsValue == 0 || target.Flags&ast.SymbolFlagsConstEnum != 0 {
				continue
			}
			reference := 0
			if target.IsExternalModule() {
				reference = b.moduleNamespaceReference(n, target)
			} else {
				symbol := b.symbolFor(export, ast.SymbolName(export))
				reference = b.node(3, n, ast.SymbolName(export))
				b.nodes[reference-1].symbol = symbol
				typ := b.symbols[symbol-1].typ
				if b.symbols[symbol-1].function != 0 {
					typ = b.typ(b.c.GetAnyType())
				}
				b.nodes[reference-1].typ = typ
			}
			function, value := b.newFunction(n, "")
			body := b.node(12, n, "module getter")
			result := b.node(14, n, "return")
			b.nodes[result-1].left = reference
			b.nodes[body-1].children = result
			b.nodes[function-1].left = body
			property := b.node(5, n, ast.SymbolName(export))
			b.nodes[property-1].name = b.text(ast.SymbolName(export))
			b.nodes[property-1].left = value
			b.nodes[property-1].typ = b.typ(b.c.GetAnyType())
			marker := b.node(20, n, "getter")
			b.nodes[marker-1].constant = 1
			b.nodes[marker-1].typ = b.typ(b.c.GetUndefinedType())
			b.nodes[property-1].third = marker
			if previous == 0 {
				b.nodes[object-1].children = property
			} else {
				b.nodes[previous-1].next = property
			}
			previous = property
		}
		// An empty namespace must still use dynamic object storage.
		if previous == 0 {
			empty := b.node(20, n, "undefined")
			b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
			spread := b.node(35, n, "...")
			b.nodes[spread-1].left = empty
			b.nodes[spread-1].typ = b.typ(b.c.GetAnyType())
			b.nodes[object-1].children = spread
		}
		variable := b.node(13, n, "module namespace")
		b.nodes[variable-1].symbol = b.moduleNamespaces[module.Declarations[0]]
		b.nodes[variable-1].typ = b.typ(b.c.GetAnyType())
		b.nodes[variable-1].left = object
		if last == 0 {
			b.nodes[block-1].children = variable
		} else {
			b.nodes[last-1].next = variable
		}
		last = variable
	}
	b.nodes[block-1].next = b.statements
	b.statements = block
}

package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
)

func (b *irBuilder) prepareJSONModules(files []*ast.SourceFile) {
	b.jsonModules = map[*ast.SourceFile]int{}
	b.jsonExports = map[*ast.SourceFile]*ast.Symbol{}
	for _, file := range files {
		if !ast.IsJsonSourceFile(file) {
			continue
		}
		id := b.moduleSymbol("JSON default")
		b.jsonModules[file] = id
		if symbol := file.AsNode().Symbol(); symbol != nil {
			if exported := symbol.Exports[ast.InternalSymbolNameExportEquals]; exported != nil {
				b.symbolIDs[exported] = id
			}
		}
		exported := &ast.Symbol{Name: "default", Flags: ast.SymbolFlagsProperty, ValueDeclaration: file.AsNode(), Declarations: []*ast.Node{file.AsNode()}}
		b.jsonExports[file] = exported
		b.symbolIDs[exported] = id
	}
}
func (b *irBuilder) jsonModuleInitializer(file *ast.SourceFile) int {
	n := file.AsNode()
	value := b.lower(file.Statements.Nodes[0].Expression())
	variable := b.node(13, n, "JSON default")
	b.nodes[variable-1].symbol = b.jsonModules[file]
	b.nodes[variable-1].typ = b.typ(b.c.GetAnyType())
	b.nodes[variable-1].left = value
	return variable
}

func (b *irBuilder) jsonNamespaceImport(symbol *ast.Symbol) *ast.SourceFile {
	for _, declaration := range symbol.Declarations {
		if declaration.Kind != ast.KindNamespaceImport {
			continue
		}
		for parent := declaration.Parent; parent != nil; parent = parent.Parent {
			if parent.Kind == ast.KindImportDeclaration {
				target := b.moduleAliases[b.file][parent.ModuleSpecifier().Text()]
				if target != nil && ast.IsJsonSourceFile(target) {
					return target
				}
				break
			}
		}
	}
	return nil
}

func validateModuleAttributes(specifier *ast.Node, target *ast.SourceFile) {
	declaration := specifier.Parent
	var attributes *ast.Node
	var bindings *ast.Node
	importing := false
	switch declaration.Kind {
	case ast.KindImportDeclaration:
		importing = true
		attributes = declaration.AsImportDeclaration().Attributes
		if clause := declaration.AsImportDeclaration().ImportClause; clause != nil {
			bindings = clause.AsImportClause().NamedBindings
		}
	case ast.KindExportDeclaration:
		attributes = declaration.AsExportDeclaration().Attributes
		bindings = declaration.AsExportDeclaration().ExportClause
	default:
		return
	}
	if attributes != nil {
		for _, attribute := range attributes.AsImportAttributes().Attributes.Nodes {
			if attribute.Name().Text() != "type" || attribute.AsImportAttribute().Value.Text() != "json" || !ast.IsJsonSourceFile(target) {
				panic("unsupported module import attribute")
			}
		}
	}
	if bindings != nil && bindings.Kind == ast.KindNamespaceExport && ast.IsJsonSourceFile(target) {
		panic("JSON namespace re-exports are not supported")
	}
	if ast.IsJsonSourceFile(target) && (bindings == nil && !importing || bindings != nil && bindings.Kind != ast.KindNamespaceImport) {
		if bindings == nil {
			panic("JSON modules expose only a default export")
		}
		for _, binding := range bindings.Elements() {
			if binding.IsTypeOnly() {
				continue
			}
			var name *ast.Node
			if importing {
				name = binding.AsImportSpecifier().PropertyName
			} else {
				name = binding.AsExportSpecifier().PropertyName
			}
			if name == nil {
				name = binding.Name()
			}
			if name.Text() != "default" {
				panic("JSON modules expose only a default export")
			}
		}
	}
}

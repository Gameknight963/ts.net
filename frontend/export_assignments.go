package main

import "github.com/microsoft/typescript-go/internal/ast"

func (b *irBuilder) prepareExportAssignments(files []*ast.SourceFile) {
	values := map[*ast.SourceFile]int{}
	b.moduleAssignments = values
	for _, file := range files {
		if ast.IsJsonSourceFile(file) {
			continue
		}
		for _, statement := range file.Statements.Nodes {
			if statement.Kind != ast.KindExportAssignment || !statement.AsExportAssignment().IsExportEquals {
				continue
			}
			export := file.AsNode().Symbol().Exports[ast.InternalSymbolNameExportEquals]
			id := b.moduleSymbol("export assignment")
			b.symbolIDs[export] = id
			values[file] = id
		}
	}
	for _, file := range files {
		for _, statement := range file.Statements.Nodes {
			if statement.Kind != ast.KindImportEqualsDeclaration || !b.importAliases[statement] {
				continue
			}
			reference := statement.AsImportEqualsDeclaration().ModuleReference
			if reference.Kind != ast.KindExternalModuleReference {
				continue
			}
			if id := values[b.moduleAliases[file][reference.Expression().Text()]]; id != 0 {
				b.symbolIDs[b.c.GetSymbolAtLocation(statement.Name())] = id
			}
		}
	}
}

func (b *irBuilder) exportAssignmentReference(n *ast.Node, symbol *ast.Symbol) int {
	seen := map[*ast.Symbol]bool{}
	for symbol != nil && !seen[symbol] {
		seen[symbol] = true
		for _, declaration := range symbol.Declarations {
			var specifier *ast.Node
			member := ""
			if declaration.Kind == ast.KindImportEqualsDeclaration {
				reference := declaration.AsImportEqualsDeclaration().ModuleReference
				if reference.Kind == ast.KindExternalModuleReference {
					specifier = reference.Expression()
				}
			} else if declaration.Kind == ast.KindImportClause || declaration.Kind == ast.KindImportSpecifier || declaration.Kind == ast.KindNamespaceImport || declaration.Kind == ast.KindExportSpecifier {
				for parent := declaration.Parent; parent != nil; parent = parent.Parent {
					if parent.Kind == ast.KindImportDeclaration || parent.Kind == ast.KindExportDeclaration {
						specifier = parent.ModuleSpecifier()
						break
					}
				}
				if declaration.Kind == ast.KindImportSpecifier {
					member = declaration.AsImportSpecifier().PropertyNameOrName().Text()
				} else if declaration.Kind == ast.KindExportSpecifier {
					member = declaration.AsExportSpecifier().PropertyNameOrName().Text()
				}
			}
			if specifier == nil {
				continue
			}
			file := ast.GetSourceFileOfNode(declaration)
			id := b.moduleAssignments[b.moduleAliases[file][specifier.Text()]]
			if id == 0 {
				continue
			}
			reference := b.moduleRef(n, id)
			if member == "" || member == "default" {
				return reference
			}
			property := b.node(5, n, member)
			b.nodes[property-1].name = b.text(member)
			b.nodes[property-1].left = reference
			b.nodes[property-1].typ = b.typ(b.c.GetAnyType())
			return property
		}
		if symbol.Flags&ast.SymbolFlagsAlias == 0 {
			break
		}
		symbol = b.c.GetImmediateAliasedSymbol(symbol)
	}
	return 0
}

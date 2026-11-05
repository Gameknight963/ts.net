package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
)

// Let the frontend decide which imports survive TypeScript's type erasure.
func (b *irBuilder) runtimeImport(n *ast.Node) bool {
	resolver := b.c.GetEmitResolver()
	if n.Kind == ast.KindExportDeclaration {
		e := n.AsExportDeclaration()
		if e.IsTypeOnly {
			return false
		}
		if e.ExportClause == nil {
			return true
		}
		return resolver.IsValueAliasDeclaration(n)
	}
	clause := n.AsImportDeclaration().ImportClause
	if clause == nil {
		return true
	}
	if clause.IsTypeOnly() {
		return false
	}
	if clause.Name() != nil && resolver.IsReferencedAliasDeclaration(clause) {
		return true
	}
	bindings := clause.AsImportClause().NamedBindings
	if bindings == nil {
		return false
	}
	if bindings.Kind == ast.KindNamespaceImport {
		return resolver.IsReferencedAliasDeclaration(bindings)
	}
	for _, binding := range bindings.Elements() {
		if !binding.IsTypeOnly() && resolver.IsReferencedAliasDeclaration(binding) {
			return true
		}
	}
	return false
}

func (b *irBuilder) moduleFiles(s *session) []*ast.SourceFile {
	var ordered []*ast.SourceFile
	visited := map[*ast.SourceFile]bool{}
	var visit func(*ast.SourceFile)
	visit = func(file *ast.SourceFile) {
		if visited[file] {
			return
		}
		visited[file] = true
		b.file = file
		b.c.GetEmitResolver().MarkLinkedReferencesRecursively(file)
		for _, n := range file.Statements.Nodes {
			if n.Kind != ast.KindImportDeclaration && n.Kind != ast.KindExportDeclaration {
				continue
			}
			specifier := n.ModuleSpecifier()
			if specifier == nil || !b.runtimeImport(n) {
				continue
			}
			resolved := s.program.GetResolvedModuleFromModuleSpecifier(file, specifier)
			if resolved == nil {
				panic("unresolved runtime module: " + specifier.Text())
			}
			dependency := s.program.GetSourceFile(resolved.ResolvedFileName)
			if dependency == nil || dependency.IsDeclarationFile {
				panic("runtime module requires TypeScript source: " + specifier.Text())
			}
			visit(dependency)
			b.file = file
		}
		ordered = append(ordered, file)
	}
	visit(s.file)
	return ordered
}

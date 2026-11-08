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
	b.moduleAsync = map[*ast.SourceFile]bool{}
	visited := map[*ast.SourceFile]bool{}
	b.moduleDependencies = map[*ast.SourceFile][]*ast.SourceFile{}
	b.moduleAliases = map[*ast.SourceFile]map[string]*ast.SourceFile{}
	var visit func(*ast.SourceFile)
	visit = func(file *ast.SourceFile) {
		if visited[file] {
			return
		}
		visited[file] = true
		b.moduleAsync[file] = hasModuleAwait(file.AsNode())
		if b.moduleAsync[file] {
			b.moduleLoader = true
		}
		b.file = file
		b.c.GetEmitResolver().MarkLinkedReferencesRecursively(file)
		dependency := func(specifier *ast.Node, static bool) {
			resolved := s.program.GetResolvedModuleFromModuleSpecifier(file, specifier)
			if resolved == nil {
				panic("unresolved runtime module: " + specifier.Text())
			}
			target := s.program.GetSourceFile(resolved.ResolvedFileName)
			if target == nil || target.IsDeclarationFile {
				panic("runtime module requires TypeScript source: " + specifier.Text())
			}
			validateModuleAttributes(specifier, target)
			if b.moduleAliases[file] == nil {
				b.moduleAliases[file] = map[string]*ast.SourceFile{}
			}
			b.moduleAliases[file][specifier.Text()] = target
			if static {
				b.moduleDependencies[file] = append(b.moduleDependencies[file], target)
			}
			visit(target)
			b.file = file
		}
		for _, n := range file.Statements.Nodes {
			if (n.Kind == ast.KindImportDeclaration || n.Kind == ast.KindExportDeclaration) && n.ModuleSpecifier() != nil && b.runtimeImport(n) {
				dependency(n.ModuleSpecifier(), true)
			}
			if n.Kind == ast.KindImportEqualsDeclaration && !n.AsImportEqualsDeclaration().IsTypeOnly && b.c.GetEmitResolver().IsReferencedAliasDeclaration(n) {
				reference := n.AsImportEqualsDeclaration().ModuleReference
				if reference.Kind == ast.KindExternalModuleReference {
					dependency(reference.Expression(), true)
				}
			}
		}
		var scan func(*ast.Node)
		scan = func(n *ast.Node) {
			if n.Kind == ast.KindCallExpression && n.AsCallExpression().Expression.Kind == ast.KindImportKeyword {
				b.moduleLoader = true
				args := n.AsCallExpression().Arguments.Nodes
				if len(args) != 0 && (args[0].Kind == ast.KindStringLiteral || args[0].Kind == ast.KindNoSubstitutionTemplateLiteral) {
					dependency(args[0], false)
				}
			}
			n.ForEachChild(func(child *ast.Node) bool { scan(child); return false })
		}
		if file.Flags&ast.NodeFlagsPossiblyContainsDynamicImport != 0 {
			scan(file.AsNode())
		}
		ordered = append(ordered, file)
	}
	visit(s.file)
	return ordered
}

// Export assignments evaluate a value once. Export specifiers keep live aliases.
func defaultSnapshot(symbol *ast.Symbol) bool {
	for _, declaration := range symbol.Declarations {
		if declaration.Kind == ast.KindExportAssignment && !declaration.AsExportAssignment().IsExportEquals {
			return true
		}
	}
	return false
}

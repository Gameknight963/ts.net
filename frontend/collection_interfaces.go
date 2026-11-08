package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"strings"
)

// Native collections retain their runtime identity through derived TS interfaces.
func (b *irBuilder) collectionInterfaceType(t *checker.Type) bool {
	if t == nil || t.Flags()&checker.TypeFlagsObject == 0 {
		return false
	}
	if symbol := t.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsClass != 0 {
		return false
	}
	if t.ObjectFlags()&checker.ObjectFlagsMapped != 0 && b.mappedCollectionSource(t, map[*checker.Type]bool{}) {
		return true
	}
	if t.ObjectFlags()&checker.ObjectFlagsReference != 0 {
		t = t.Target()
	}
	if t == nil || t.ObjectFlags()&checker.ObjectFlagsInterface == 0 {
		return false
	}
	return b.collectionInterfaceBase(t, map[*checker.Type]bool{})
}
func (b *irBuilder) collectionInterfaceBase(t *checker.Type, seen map[*checker.Type]bool) bool {
	if t == nil || t.Flags()&checker.TypeFlagsObject == 0 || seen[t] {
		return false
	}
	seen[t] = true
	if symbol := t.Symbol(); symbol != nil && collectionType(symbol.Name) {
		for _, declaration := range symbol.Declarations {
			if file := ast.GetSourceFileOfNode(declaration); file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/") {
				return true
			}
		}
	}
	if t.ObjectFlags()&checker.ObjectFlagsReference != 0 {
		t = t.Target()
	}
	if t == nil || t.ObjectFlags()&checker.ObjectFlagsClassOrInterface == 0 {
		return false
	}
	for _, base := range b.c.GetBaseTypes(t) {
		if b.collectionInterfaceBase(base, seen) {
			return true
		}
	}
	return false
}

// Named aliases can hide the instantiated mapped-type arguments. Ask the checker
// about their referenced source types rather than reconstructing the mapped type.
func (b *irBuilder) collectionAliasSource(symbol *ast.Symbol, seen map[*ast.Symbol]bool) bool {
	if symbol == nil || seen[symbol] {
		return false
	}
	seen[symbol] = true
	if symbol.Flags&ast.SymbolFlagsAlias != 0 {
		return b.collectionAliasSource(b.c.GetAliasedSymbol(symbol), seen)
	}
	for _, declaration := range symbol.Declarations {
		if declaration.Kind != ast.KindTypeAliasDeclaration {
			continue
		}
		var visit func(*ast.Node) bool
		visit = func(n *ast.Node) bool {
			// A collection inside a field or callback is not the mapped source.
			switch n.Kind {
			case ast.KindTypeLiteral, ast.KindFunctionType, ast.KindConstructorType, ast.KindTupleType:
				return false
			}
			if n.Kind == ast.KindTypeReference {
				if b.collectionInterfaceBase(b.c.GetTypeAtLocation(n), map[*checker.Type]bool{}) {
					return true
				}
				if b.collectionAliasSource(b.c.GetSymbolAtLocation(n.AsTypeReferenceNode().TypeName), seen) {
					return true
				}
			}
			return n.ForEachChild(visit)
		}
		if visit(declaration.Type()) {
			return true
		}
	}
	return false
}

func (b *irBuilder) mappedCollectionSource(t *checker.Type, seen map[*checker.Type]bool) bool {
	if t == nil || seen[t] {
		return false
	}
	seen[t] = true
	if b.collectionInterfaceBase(t, map[*checker.Type]bool{}) {
		return true
	}
	if t.ObjectFlags()&checker.ObjectFlagsMapped == 0 {
		return false
	}
	alias := t.Alias()
	if alias == nil {
		return false
	}
	if b.collectionAliasSource(alias.Symbol(), map[*ast.Symbol]bool{}) {
		return true
	}
	for _, argument := range alias.TypeArguments() {
		if b.mappedCollectionSource(argument, seen) {
			return true
		}
	}
	return false
}

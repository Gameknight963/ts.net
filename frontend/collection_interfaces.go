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
	if t.ObjectFlags()&checker.ObjectFlagsReference != 0 {
		t = t.Target()
	}
	if t == nil || t.ObjectFlags()&checker.ObjectFlagsInterface == 0 {
		return false
	}
	return b.collectionInterfaceBase(t, map[*checker.Type]bool{})
}
func (b *irBuilder) collectionInterfaceBase(t *checker.Type, seen map[*checker.Type]bool) bool {
	if t == nil || seen[t] {
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
	if t == nil || t.ObjectFlags()&checker.ObjectFlagsInterface == 0 {
		return false
	}
	for _, base := range b.c.GetBaseTypes(t) {
		if b.collectionInterfaceBase(base, seen) {
			return true
		}
	}
	return false
}

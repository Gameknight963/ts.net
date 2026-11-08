package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// Promise protocols keep the original promise/thenable identity. Native promises
// do not implement generated CLR shape interfaces for user-written TS interfaces.
func (b *irBuilder) thenableType(t *checker.Type) bool {
	if t == nil || t.Flags()&(checker.TypeFlagsObject|checker.TypeFlagsIntersection) == 0 {
		return false
	}
	if symbol := t.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsClass != 0 {
		return false
	}
	then := b.c.GetTypeOfPropertyOfType(t, "then")
	if then == nil {
		return false
	}
	parts := []*checker.Type{then}
	if then.Flags()&checker.TypeFlagsUnion != 0 {
		parts = then.Types()
	}
	callable := false
	for _, part := range parts {
		if part.Flags()&(checker.TypeFlagsNull|checker.TypeFlagsUndefined) != 0 {
			continue
		}
		if len(b.c.GetSignaturesOfType(part, checker.SignatureKindCall)) == 0 {
			return false
		}
		callable = true
	}
	return callable
}

package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// Sequence protocols use the original array, view, string, or object. Requiring
// a generated CLR interface would reject native values already checked by TS.
func (b *irBuilder) arrayLikeType(t *checker.Type) bool {
	if t == nil || t.Flags()&(checker.TypeFlagsObject|checker.TypeFlagsIntersection) == 0 {
		return false
	}
	if symbol := t.Symbol(); symbol != nil && symbol.Flags&ast.SymbolFlagsClass != 0 {
		return false
	}
	if b.c.GetIndexInfoOfType(t, b.c.GetNumberType()) == nil {
		return false
	}
	length := b.c.GetTypeOfPropertyOfType(t, "length")
	if length == nil {
		return false
	}
	numericLength := length.Flags()&checker.TypeFlagsNumberLike != 0
	if length.Flags()&checker.TypeFlagsUnion != 0 {
		for _, part := range length.Types() {
			if part.Flags()&checker.TypeFlagsNumberLike != 0 {
				numericLength = true
			} else if part.Flags()&checker.TypeFlagsUndefined == 0 {
				return false
			}
		}
	}
	if !numericLength {
		return false
	}
	for _, property := range b.c.GetPropertiesOfType(t) {
		if !checker.IsKnownSymbol(property) && ast.SymbolName(property) != "length" && property.Flags&ast.SymbolFlagsOptional == 0 {
			return false
		}
	}
	return true
}

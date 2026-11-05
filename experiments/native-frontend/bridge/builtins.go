package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"strings"
)

func (b *irBuilder) isBuiltinHasOwn(n *ast.Node) bool {
	if n.Kind != ast.KindPropertyAccessExpression || n.Name().Text() != "hasOwn" || n.Expression().Kind != ast.KindIdentifier || n.Expression().Text() != "Object" {
		return false
	}
	symbol := b.c.GetSymbolAtLocation(n.Expression())
	if symbol == nil || symbol.ValueDeclaration == nil {
		return false
	}
	file := ast.GetSourceFileOfNode(symbol.ValueDeclaration)
	return file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/")
}

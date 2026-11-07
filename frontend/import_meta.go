package main

import (
	"net/url"
	"path/filepath"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
)

func sourceFileURL(file *ast.SourceFile) string {
	path := filepath.ToSlash(file.FileName())
	if !strings.HasPrefix(path, "/") {
		path = "/" + path
	}
	return (&url.URL{Scheme: "file", Path: path}).String()
}

func (b *irBuilder) importMeta(n *ast.Node) int {
	sourceURL := sourceFileURL(b.file)
	helper := b.node(66, n, "__importMeta")
	b.nodes[helper-1].typ = b.typ(b.c.GetAnyType())
	argument := b.node(1, n, sourceURL)
	b.nodes[argument-1].typ = b.typ(b.c.GetStringType())
	call := b.node(6, n, "(")
	b.nodes[call-1].left = helper
	b.nodes[call-1].children = argument
	b.nodes[call-1].typ = b.typ(b.c.GetAnyType())
	return call
}

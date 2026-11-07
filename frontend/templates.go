package main

import (
	"fmt"
	"strings"

	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/scanner"
)

func (b *irBuilder) taggedTemplate(n *ast.Node) int {
	tag := n.AsTaggedTemplateExpression()
	literals := []*ast.Node{}
	expressions := []*ast.Node{}
	if tag.Template.Kind == ast.KindNoSubstitutionTemplateLiteral {
		literals = append(literals, tag.Template)
	} else {
		template := tag.Template.AsTemplateExpression()
		literals = append(literals, template.Head)
		for _, part := range template.TemplateSpans.Nodes {
			span := part.AsTemplateSpan()
			expressions = append(expressions, span.Expression)
			literals = append(literals, span.Literal)
		}
	}
	anyType := b.typ(b.c.GetAnyType())
	cooked := b.node(32, n, "[")
	raw := b.node(32, n, "[")
	b.nodes[cooked-1].typ = anyType
	b.nodes[raw-1].typ = anyType
	previousCooked, previousRaw := 0, 0
	for _, literal := range literals {
		value := b.node(1, literal, literal.Text())
		b.nodes[value-1].typ = b.typ(b.c.GetStringType())
		if literal.TemplateLiteralLikeData().TemplateFlags&ast.TokenFlagsContainsInvalidEscape != 0 {
			b.nodes[value-1].kind = 20
			b.nodes[value-1].typ = b.typ(b.c.GetUndefinedType())
		}
		start := scanner.GetTokenPosOfNode(literal, b.file, false)
		end := literal.End() - 1
		if literal.Kind == ast.KindTemplateHead || literal.Kind == ast.KindTemplateMiddle {
			end--
		}
		text := b.file.Text()[start+1 : end]
		text = strings.ReplaceAll(strings.ReplaceAll(text, "\r\n", "\n"), "\r", "\n")
		rawValue := b.node(1, literal, text)
		b.nodes[rawValue-1].typ = b.typ(b.c.GetStringType())
		if previousCooked == 0 {
			b.nodes[cooked-1].children = value
			b.nodes[raw-1].children = rawValue
		} else {
			b.nodes[previousCooked-1].next = value
			b.nodes[previousRaw-1].next = rawValue
		}
		previousCooked = value
		previousRaw = rawValue
	}
	helper := b.node(66, n, "__templateObject")
	b.nodes[helper-1].typ = anyType
	key := b.node(1, n, fmt.Sprintf("%s:%d", b.file.FileName(), n.Pos()))
	b.nodes[key-1].typ = b.typ(b.c.GetStringType())
	b.nodes[key-1].next = cooked
	b.nodes[cooked-1].next = raw
	site := b.node(6, n, "(")
	b.nodes[site-1].left = helper
	b.nodes[site-1].children = key
	b.nodes[site-1].typ = anyType
	result := b.node(6, n, "(")
	b.nodes[result-1].left = b.lower(tag.Tag)
	b.nodes[result-1].children = site
	b.nodes[result-1].typ = b.typ(b.c.GetTypeAtLocation(n))
	previous := site
	for _, expression := range expressions {
		value := b.lower(expression)
		b.nodes[previous-1].next = value
		previous = value
	}
	return result
}

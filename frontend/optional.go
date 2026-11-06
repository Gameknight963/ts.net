package main

import "github.com/microsoft/typescript-go/internal/ast"

func chainNode(n *ast.Node) bool {
	return n != nil && n.Flags&ast.NodeFlagsOptionalChain != 0 && (n.Kind == ast.KindPropertyAccessExpression || n.Kind == ast.KindElementAccessExpression || n.Kind == ast.KindCallExpression || n.Kind == ast.KindNonNullExpression)
}
func (b *irBuilder) optionalChain(n *ast.Node) int {
	nodes := []*ast.Node{}
	base := n
	for chainNode(base) {
		nodes = append(nodes, base)
		base = base.Expression()
	}
	root := b.node(62, n, "optional chain")
	value := b.lower(base)
	b.nodes[root-1].left = value
	b.nodes[root-1].typ = b.typ(b.c.GetTypeAtLocation(n))
	last := 0
	for index := len(nodes) - 1; index >= 0; index-- {
		node := nodes[index]
		if node.Kind == ast.KindNonNullExpression {
			continue
		}
		kind := 63
		if node.Kind == ast.KindElementAccessExpression {
			kind = 64
		} else if node.Kind == ast.KindCallExpression {
			kind = 65
		}
		entry := b.node(kind, node, "chain step")
		b.nodes[entry-1].typ = b.typ(b.c.GetTypeAtLocation(node))
		switch node.Kind {
		case ast.KindPropertyAccessExpression:
			b.nodes[entry-1].name = b.text(node.Name().Text())
			if node.AsPropertyAccessExpression().QuestionDotToken != nil {
				b.nodes[entry-1].constant = 1
			}
			marker := b.node(20, node, "receiver type")
			b.nodes[marker-1].typ = b.typ(b.c.GetNonNullableType(b.c.GetTypeAtLocation(node.Expression())))
			b.nodes[entry-1].fourth = marker
		case ast.KindElementAccessExpression:
			key := b.lower(node.AsElementAccessExpression().ArgumentExpression)
			b.nodes[entry-1].right = key
			if node.AsElementAccessExpression().QuestionDotToken != nil {
				b.nodes[entry-1].constant = 1
			}
		case ast.KindCallExpression:
			if node.AsCallExpression().Arguments != nil {
				args := b.list(node.AsCallExpression().Arguments.Nodes)
				b.nodes[entry-1].children = args
			}
			if node.AsCallExpression().QuestionDotToken != nil {
				b.nodes[entry-1].constant = 1
			}
		}
		if last == 0 {
			b.nodes[root-1].children = entry
		} else {
			b.nodes[last-1].next = entry
		}
		last = entry
	}
	return root
}

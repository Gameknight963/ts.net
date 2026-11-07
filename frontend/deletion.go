package main

import "github.com/microsoft/typescript-go/internal/ast"

// A deleted fixed field cannot retain its CLR layout. Conservatively keep all
// literals containing a potentially deleted name in dynamic storage, including
// aliases passed across functions and modules. Unknown keys cover every literal.
func (b *irBuilder) prepareDeletion(files []*ast.SourceFile) {
	b.deletedNames = map[string]bool{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindIdentifier && n.Text() == "Reflect" && b.isBuiltinIdentifier(n) {
			access := n.Parent
			method := ""
			if access != nil && access.Kind == ast.KindPropertyAccessExpression && access.Expression() == n {
				method = access.Name().Text()
			} else if access != nil && access.Kind == ast.KindElementAccessExpression && access.Expression() == n {
				key := access.AsElementAccessExpression().ArgumentExpression
				if key.Kind == ast.KindStringLiteral {
					method = key.Text()
				}
			}
			if method == "" {
				// An escaping namespace or unknown member may expose deleteProperty
				// through aliases, destructuring, callbacks or another module.
				b.deleteAnyName = true
			} else if method == "deleteProperty" {
				call := access.Parent
				if call != nil && call.Kind == ast.KindCallExpression && call.Expression() == access && len(call.AsCallExpression().Arguments.Nodes) >= 2 {
					key := call.AsCallExpression().Arguments.Nodes[1]
					if key.Kind == ast.KindStringLiteral {
						b.deletedNames[key.Text()] = true
					} else {
						b.deleteAnyName = true
					}
				} else {
					b.deleteAnyName = true
				}
			}
		}
		if n.Kind == ast.KindDeleteExpression {
			operand := n.Expression()
			for operand.Kind == ast.KindParenthesizedExpression {
				operand = operand.Expression()
			}
			if operand.Kind == ast.KindPropertyAccessExpression {
				b.deletedNames[operand.Name().Text()] = true
			} else if operand.Kind == ast.KindElementAccessExpression {
				key := operand.AsElementAccessExpression().ArgumentExpression
				if key.Kind == ast.KindStringLiteral {
					b.deletedNames[key.Text()] = true
				} else {
					b.deleteAnyName = true
				}
			}
		}
		n.ForEachChild(func(child *ast.Node) bool { visit(child); return false })
	}
	for _, file := range files {
		visit(file.AsNode())
	}
}
func (b *irBuilder) deletableLiteral(n *ast.Node, row *irNode) {
	if n.Kind != ast.KindObjectLiteralExpression || len(b.deletedNames) == 0 && !b.deleteAnyName {
		return
	}
	affected := b.deleteAnyName
	for property := row.children; property != 0; property = b.nodes[property-1].next {
		p := b.nodes[property-1]
		if p.name != 0 && b.deletedNames[b.texts[p.name-1]] {
			affected = true
		}
	}
	if !affected {
		return
	}
	empty := b.node(20, n, "undefined")
	b.nodes[empty-1].typ = b.typ(b.c.GetUndefinedType())
	spread := b.node(35, n, "...")
	b.nodes[spread-1].left = empty
	b.nodes[spread-1].next = row.children
	row.children = spread
	row.typ = b.typ(b.c.GetAnyType())
}

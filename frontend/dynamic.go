package main

import "github.com/microsoft/typescript-go/internal/ast"

// An object-backed binding can change representation during a compound write.
// A narrowed checker type must not turn that write into a numeric cast.
func (b *irBuilder) preserveDynamicType(n *ast.Node, row *irNode) {
	any := b.typ(b.c.GetAnyType())
	dynamic := func(id int) bool { return id != 0 && b.nodes[id-1].typ == any }
	switch n.Kind {
	case ast.KindIdentifier, ast.KindPropertyAccessExpression:
		location := n
		if n.Kind == ast.KindPropertyAccessExpression {
			location = n.Name()
		}
		symbol := b.c.GetSymbolAtLocation(location)
		if symbol != nil {
			kind := b.types[b.typ(b.c.GetTypeOfSymbol(symbol))-1].kind
			if kind == 4 || kind == 5 {
				row.typ = any
			}
		}
	case ast.KindElementAccessExpression:
		if row.kind != 33 {
			return
		}
		owner := b.nodes[row.left-1].typ
		if owner != 0 {
			typ := b.types[owner-1]
			if typ.kind == 4 || typ.kind == 5 || typ.kind == 7 && (b.types[typ.element-1].kind == 4 || b.types[typ.element-1].kind == 5) {
				row.typ = any
			}
		}
	case ast.KindBinaryExpression:
		op := n.AsBinaryExpression().OperatorToken.Kind
		if op == ast.KindEqualsToken {
			row.typ = b.nodes[row.right-1].typ
			return
		}
		switch op {
		case ast.KindPlusToken, ast.KindPlusEqualsToken, ast.KindAmpersandAmpersandToken, ast.KindBarBarToken, ast.KindQuestionQuestionToken,
			ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
			if dynamic(row.left) || dynamic(row.right) {
				row.typ = any
			}
		case ast.KindCommaToken:
			row.typ = b.nodes[row.right-1].typ
		}
	case ast.KindConditionalExpression:
		if dynamic(row.right) || dynamic(row.third) {
			row.typ = any
		}
	}
}

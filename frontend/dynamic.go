package main

import "github.com/microsoft/typescript-go/internal/ast"

// An object-backed binding can change representation during a compound write.
// A narrowed checker type must not turn that write into a numeric cast.
func (b *irBuilder) preserveDynamicType(n *ast.Node, row *irNode) {
	any := b.typ(b.c.GetAnyType())
	dynamic := func(id int) bool { return id != 0 && b.nodes[id-1].typ == any }
	switch n.Kind {
	case ast.KindCallExpression:
		if signature := b.c.GetResolvedSignature(n); signature != nil && b.dynamicNumericReturn(signature.Declaration(), map[*ast.Node]bool{}) {
			row.typ = any
		}

	case ast.KindVariableDeclaration:
		if n.Initializer() != nil && n.Type() == nil && dynamic(row.left) && row.typ != 0 && b.types[row.typ-1].kind == 0 {
			row.typ = any
			b.symbols[row.symbol-1].typ = any
		}
	case ast.KindIdentifier, ast.KindPropertyAccessExpression:
		location := n
		if n.Kind == ast.KindPropertyAccessExpression {
			location = n.Name()
		}
		symbol := b.c.GetSymbolAtLocation(location)
		if symbol != nil {
			if symbol.Flags&ast.SymbolFlagsEnumMember != 0 && ast.SymbolName(symbol) == "__proto__" {
				row.typ = any
				return
			}
			kind := b.types[b.typ(b.c.GetTypeOfSymbol(symbol))-1].kind
			if kind == 4 || kind == 5 || row.symbol != 0 && b.symbols[row.symbol-1].typ == any {
				row.typ = any
			}
		}
	case ast.KindElementAccessExpression:
		if row.kind != 33 {
			return
		}
		// Built-in typed arrays convert every stored element. Their indexed
		// values retain the checker's number/bigint type, including undefined
		// through the normal nullable storage for out-of-range reads.
		if source := b.c.GetTypeAtLocation(n.Expression()); source != nil && row.right != 0 && b.types[b.nodes[row.right-1].typ-1].kind == 0 {
			if symbol := source.Symbol(); symbol != nil && binaryBuiltinName(symbol.Name) {
				return
			}
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
		case ast.KindMinusToken, ast.KindAsteriskToken, ast.KindSlashToken, ast.KindPercentToken, ast.KindAsteriskAsteriskToken,
			ast.KindAmpersandToken, ast.KindBarToken, ast.KindCaretToken, ast.KindLessThanLessThanToken, ast.KindGreaterThanGreaterThanToken, ast.KindGreaterThanGreaterThanGreaterThanToken,
			ast.KindMinusEqualsToken, ast.KindAsteriskEqualsToken, ast.KindSlashEqualsToken, ast.KindPercentEqualsToken, ast.KindAsteriskAsteriskEqualsToken,
			ast.KindAmpersandEqualsToken, ast.KindBarEqualsToken, ast.KindCaretEqualsToken, ast.KindLessThanLessThanEqualsToken, ast.KindGreaterThanGreaterThanEqualsToken,
			ast.KindGreaterThanGreaterThanGreaterThanEqualsToken:
			if dynamic(row.left) && dynamic(row.right) {
				row.typ = any
			}
		case ast.KindPlusToken, ast.KindPlusEqualsToken, ast.KindAmpersandAmpersandToken, ast.KindBarBarToken, ast.KindQuestionQuestionToken,
			ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
			if dynamic(row.left) || dynamic(row.right) {
				row.typ = any
			}
		case ast.KindCommaToken:
			row.typ = b.nodes[row.right-1].typ
		}
	case ast.KindPrefixUnaryExpression, ast.KindPostfixUnaryExpression:
		if dynamic(row.left) && (row.kind == 11 || row.kind == 8 && n.Kind == ast.KindPrefixUnaryExpression && (n.AsPrefixUnaryExpression().Operator == ast.KindMinusToken || n.AsPrefixUnaryExpression().Operator == ast.KindTildeToken)) {
			row.typ = any
		}
	case ast.KindConditionalExpression:
		if dynamic(row.right) || dynamic(row.third) {
			row.typ = any
		}
	}
}

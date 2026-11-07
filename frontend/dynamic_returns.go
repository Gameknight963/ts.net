package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"strings"
)

// TypeScript infers number for arithmetic on any, although its runtime value
// can be bigint. Keep inferred return storage general for those expressions.
func (b *irBuilder) dynamicNumericReturn(n *ast.Node, active map[*ast.Node]bool) (result bool) {
	if n == nil || n.Body() == nil || n.Type() != nil || active[n] {
		return false
	}
	if b.c.GetReturnTypeOfSignature(b.c.GetSignatureFromDeclaration(n)).Flags()&checker.TypeFlagsNumberLike == 0 {
		return false
	}
	if cached, exists := b.dynamicNumericReturns[n]; exists {
		return cached
	}
	if b.dynamicNumericReturns == nil {
		b.dynamicNumericReturns = map[*ast.Node]bool{}
	}
	active[n] = true
	defer func() { delete(active, n); b.dynamicNumericReturns[n] = result }()
	var expression func(*ast.Node) bool
	expression = func(value *ast.Node) bool {
		if value == nil {
			return false
		}
		if b.c.GetTypeAtLocation(value).Flags()&(checker.TypeFlagsAny|checker.TypeFlagsBigIntLike) != 0 {
			return true
		}
		switch value.Kind {
		case ast.KindParenthesizedExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
			return expression(value.Expression())
		case ast.KindIdentifier:
			symbol := b.c.GetSymbolAtLocation(value)
			if symbol != nil && symbol.ValueDeclaration != nil {
				declaration := symbol.ValueDeclaration
				if declaration.Kind == ast.KindVariableDeclaration && declaration.Type() == nil {
					return expression(declaration.Initializer())
				}
			}
		case ast.KindCallExpression:
			if signature := b.c.GetResolvedSignature(value); signature != nil {
				return b.dynamicNumericReturn(signature.Declaration(), active)
			}
		case ast.KindConditionalExpression:
			condition := value.AsConditionalExpression()
			return expression(condition.WhenTrue) || expression(condition.WhenFalse)
		case ast.KindBinaryExpression:
			binary := value.AsBinaryExpression()
			switch strings.TrimSuffix(operators[binary.OperatorToken.Kind], "=") {
			case "+", "-", "*", "/", "%", "**", "&", "|", "^", "<<", ">>":
				return expression(binary.Left) && expression(binary.Right)
			}
		case ast.KindPrefixUnaryExpression:
			unary := value.AsPrefixUnaryExpression()
			switch operators[unary.Operator] {
			case "-", "~", "++", "--":
				return expression(unary.Operand)
			}
		case ast.KindPostfixUnaryExpression:
			return expression(value.AsPostfixUnaryExpression().Operand)
		}
		return false
	}
	if n.Body().Kind != ast.KindBlock {
		return expression(n.Body())
	}
	var visit func(*ast.Node) bool
	visit = func(node *ast.Node) bool {
		switch node.Kind {
		case ast.KindFunctionDeclaration, ast.KindFunctionExpression, ast.KindArrowFunction, ast.KindMethodDeclaration, ast.KindGetAccessor, ast.KindSetAccessor, ast.KindConstructor:
			return false
		case ast.KindReturnStatement:
			return expression(node.Expression())
		}
		return node.ForEachChild(visit)
	}
	return visit(n.Body())
}

func (b *irBuilder) functionResultType(n *ast.Node) int {
	if b.dynamicNumericReturn(n, map[*ast.Node]bool{}) {
		return b.typ(b.c.GetAnyType())
	}
	return b.typ(b.c.GetReturnTypeOfSignature(b.c.GetSignatureFromDeclaration(n)))
}

// A later var declaration can widen shared storage. Earlier declarations and
// reads must use that same storage representation when the IR is emitted.
func (b *irBuilder) normalizeDynamicBindings() {
	any := b.typ(b.c.GetAnyType())
	for index := range b.nodes {
		node := &b.nodes[index]
		if node.symbol == 0 || (node.kind != 3 && node.kind != 13) {
			continue
		}
		symbol := b.symbols[node.symbol-1]
		if symbol.function == 0 && symbol.typ == any {
			node.typ = any
		}
	}
}

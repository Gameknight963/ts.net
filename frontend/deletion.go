package main

import "github.com/microsoft/typescript-go/internal/ast"

func deletionAccess(n *ast.Node) (*ast.Node, string) {
	if n.Kind == ast.KindPropertyAccessExpression {
		return n.Expression(), n.Name().Text()
	}
	if n.Kind == ast.KindElementAccessExpression {
		key := n.AsElementAccessExpression().ArgumentExpression
		if key.Kind == ast.KindStringLiteral {
			return n.Expression(), key.Text()
		}
	}
	return nil, ""
}
func (b *irBuilder) reflectionDeletionArguments(n *ast.Node) []*ast.Node {
	callee, arguments := n.Expression(), n.AsCallExpression().Arguments.Nodes
	if b.reflectionDeletionValue(callee, map[*ast.Node]bool{}) == 2 {
		return arguments
	}
	owner, method := deletionAccess(callee)
	if owner == nil || method != "call" && method != "apply" || b.reflectionDeletionValue(owner, map[*ast.Node]bool{}) != 2 {
		return nil
	}
	if method == "call" && len(arguments) >= 1 {
		return arguments[1:]
	}
	if method == "apply" && len(arguments) >= 2 && arguments[1].Kind == ast.KindArrayLiteralExpression {
		items := arguments[1].AsArrayLiteralExpression().Elements.Nodes
		for _, item := range items {
			if item.Kind == ast.KindSpreadElement || item.Kind == ast.KindOmittedExpression {
				return nil
			}
		}
		return items
	}
	return nil
}

// Recognize namespace and method aliases only at call sites. Merely retaining
// a reference to Reflect must not change unrelated object layouts.
func (b *irBuilder) reflectionDeletionValue(n *ast.Node, seen map[*ast.Node]bool) int {
	if n == nil || seen[n] {
		return 0
	}
	seen[n] = true
	switch n.Kind {
	case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
		return b.reflectionDeletionValue(n.Expression(), seen)
	case ast.KindIdentifier:
		if n.Text() == "Reflect" && b.isBuiltinIdentifier(n) {
			return 1
		}
		symbol := b.c.GetSymbolAtLocation(n)
		if symbol == nil {
			return 0
		}
		if symbol.Flags&ast.SymbolFlagsAlias != 0 {
			symbol = b.c.GetAliasedSymbol(symbol)
		}
		if symbol == nil || symbol.ValueDeclaration == nil {
			return 0
		}
		declaration := symbol.ValueDeclaration
		if declaration.Kind == ast.KindVariableDeclaration {
			return b.reflectionDeletionValue(declaration.Initializer(), seen)
		}
		if declaration.Kind == ast.KindBindingElement && declaration.Parent.Kind == ast.KindObjectBindingPattern {
			name := declaration.AsBindingElement().PropertyName
			if name == nil {
				name = declaration.Name()
			}
			if name.Kind == ast.KindComputedPropertyName {
				name = name.Expression()
			}
			owner := declaration.Parent.Parent
			if (name.Kind == ast.KindIdentifier || name.Kind == ast.KindStringLiteral) && name.Text() == "deleteProperty" && owner != nil &&
				(owner.Kind == ast.KindVariableDeclaration || owner.Kind == ast.KindParameter) && b.reflectionDeletionValue(owner.Initializer(), seen) == 1 {
				return 2
			}
		}
	case ast.KindCallExpression:
		owner, method := deletionAccess(n.Expression())
		if method == "bind" && len(n.AsCallExpression().Arguments.Nodes) <= 1 && b.reflectionDeletionValue(owner, seen) == 2 {
			return 2
		}
	case ast.KindBinaryExpression:
		expression := n.AsBinaryExpression()
		if expression.OperatorToken.Kind == ast.KindCommaToken {
			return b.reflectionDeletionValue(expression.Right, seen)
		}
	case ast.KindPropertyAccessExpression:
		if n.Name().Text() == "deleteProperty" && b.reflectionDeletionValue(n.Expression(), seen) == 1 {
			return 2
		}
	case ast.KindElementAccessExpression:
		key := n.AsElementAccessExpression().ArgumentExpression
		if key.Kind == ast.KindStringLiteral && key.Text() == "deleteProperty" && b.reflectionDeletionValue(n.Expression(), seen) == 1 {
			return 2
		}
	}
	return 0
}

// A deleted fixed field cannot retain its CLR layout. Conservatively keep all
// literals containing a potentially deleted name in dynamic storage, including
// aliases passed across functions and modules. Unknown delete-expression keys
// cover every literal. Dynamic reflection uses the target's known field names.
func (b *irBuilder) prepareDeletion(files []*ast.SourceFile) {
	b.deletedNames = map[string]bool{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if n.Kind == ast.KindCallExpression {
			arguments := b.reflectionDeletionArguments(n)
			if len(arguments) >= 2 {
				key := arguments[1]
				if key.Kind == ast.KindStringLiteral {
					b.deletedNames[key.Text()] = true
				} else {
					for _, property := range b.c.GetPropertiesOfType(b.c.GetTypeAtLocation(arguments[0])) {
						b.deletedNames[ast.SymbolName(property)] = true
					}
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

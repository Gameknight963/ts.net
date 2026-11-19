package main

import (
	"maps"

	"github.com/microsoft/typescript-go/internal/ast"
)

// Resolve values, not asserted types. Conditions may execute code, but every
// resulting key must be known. Mutable bindings and unknown branches retain
// the conservative path. Bound the work when aliases form a large graph.
func (b *irBuilder) constantDeletionKeys(n *ast.Node) ([]string, bool) {
	remaining := 256
	var resolve func(*ast.Node, map[*ast.Node]bool) ([]string, bool)
	resolve = func(n *ast.Node, seen map[*ast.Node]bool) ([]string, bool) {
		for n != nil && !seen[n] {
			remaining--
			if remaining < 0 {
				return nil, false
			}
			seen[n] = true
			switch n.Kind {
			case ast.KindStringLiteral:
				return []string{n.Text()}, true
			case ast.KindConditionalExpression:
				expression := n.AsConditionalExpression()
				left, known := resolve(expression.WhenTrue, maps.Clone(seen))
				if !known {
					return nil, false
				}
				right, known := resolve(expression.WhenFalse, seen)
				if !known {
					return nil, false
				}
				return append(left, right...), true
			case ast.KindParenthesizedExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression, ast.KindNonNullExpression, ast.KindSatisfiesExpression:
				n = n.Expression()
			case ast.KindIdentifier:
				symbol := b.c.GetSymbolAtLocation(n)
				if symbol != nil && symbol.Flags&ast.SymbolFlagsAlias != 0 {
					symbol = b.c.GetAliasedSymbol(symbol)
				}
				if symbol == nil || symbol.ValueDeclaration == nil {
					return nil, false
				}
				declaration := symbol.ValueDeclaration
				if declaration.Kind != ast.KindVariableDeclaration || ast.GetCombinedNodeFlags(declaration)&ast.NodeFlagsConst == 0 {
					return nil, false
				}
				n = declaration.Initializer()
			default:
				return nil, false
			}
		}
		return nil, false
	}
	return resolve(n, map[*ast.Node]bool{})
}

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
	if b.reflectionDeletionValue(callee, map[*ast.Node]bool{}) >= 2 {
		return arguments
	}
	owner, method := deletionAccess(callee)
	if owner == nil || method != "call" && method != "apply" || b.reflectionDeletionValue(owner, map[*ast.Node]bool{}) < 2 {
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

// A computed member can select deleteProperty or apply. Keep that ambiguity
// through aliases so a hidden target also receives dynamic storage.
func (b *irBuilder) reflectionDeletionCallKind(n *ast.Node) int {
	callee := n.Expression()
	if kind := b.reflectionDeletionValue(callee, map[*ast.Node]bool{}); kind >= 2 {
		return kind
	}
	owner, method := deletionAccess(callee)
	if owner == nil {
		return 0
	}
	kind := b.reflectionDeletionValue(owner, map[*ast.Node]bool{})
	arguments := n.AsCallExpression().Arguments.Nodes
	if kind >= 2 && (method == "call" || method == "apply" || method == "bind" && len(arguments) >= 2) {
		return kind
	}
	if kind == 1 && method == "apply" && len(arguments) >= 1 && b.reflectionDeletionValue(arguments[0], map[*ast.Node]bool{}) >= 2 {
		return 3
	}
	return 0
}

// Values are 1 for Reflect, 2 for deleteProperty, and 3 for an unknown Reflect value.
// Recognize namespace and method aliases only at call sites. Merely retaining
// a reference to Reflect must not change unrelated object layouts.
func (b *irBuilder) reflectionDeletionValue(n *ast.Node, seen map[*ast.Node]bool) int {
	if n == nil || seen[n] {
		return 0
	}
	seen[n] = true
	switch n.Kind {
	case ast.KindAwaitExpression:
		return b.reflectionAwaitedValue(n.Expression(), seen, b.reflectionDeletionValue)
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
		if declaration.Kind == ast.KindParameter || declaration.Kind == ast.KindVariableDeclaration {
			return b.reflectionBindingValue(declaration, seen, b.reflectionDeletionValue)
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
				(owner.Kind == ast.KindVariableDeclaration || owner.Kind == ast.KindParameter) && b.reflectionBindingValue(owner, seen, b.reflectionDeletionValue) == 1 {
				return 2
			}
		}
		if declaration.Kind == ast.KindBindingElement {
			return b.reflectionBindingValue(declaration, seen, b.reflectionDeletionValue)
		}
	case ast.KindCallExpression:
		if value, resolved := b.reflectionGetValue(n, seen, b.reflectionDeletionValue, "deleteProperty"); resolved {
			return value
		}
		owner, method := deletionAccess(n.Expression())
		if method == "bind" && len(n.AsCallExpression().Arguments.Nodes) <= 1 {
			if kind := b.reflectionDeletionValue(owner, seen); kind >= 2 {
				return kind
			}
		}
		return b.reflectionReturnedValue(n, seen, b.reflectionDeletionValue)
	case ast.KindBinaryExpression:
		expression := n.AsBinaryExpression()
		if expression.OperatorToken.Kind == ast.KindCommaToken || expression.OperatorToken.Kind == ast.KindEqualsToken {
			return b.reflectionDeletionValue(expression.Right, seen)
		}
		switch expression.OperatorToken.Kind {
		case ast.KindAmpersandAmpersandToken, ast.KindBarBarToken, ast.KindQuestionQuestionToken,
			ast.KindAmpersandAmpersandEqualsToken, ast.KindBarBarEqualsToken, ast.KindQuestionQuestionEqualsToken:
			left := b.reflectionDeletionValue(expression.Left, seen)
			right := b.reflectionDeletionValue(expression.Right, seen)
			if right > left {
				return right
			}
			return left
		}
	case ast.KindConditionalExpression:
		expression := n.AsConditionalExpression()
		left := b.reflectionDeletionValue(expression.WhenTrue, seen)
		right := b.reflectionDeletionValue(expression.WhenFalse, seen)
		if right > left {
			return right
		}
		return left
	case ast.KindPropertyAccessExpression:
		if !reflectionFunctionMember(n) && b.reflectionDeletionValue(n.Expression(), maps.Clone(seen)) >= 3 {
			return 3
		}
		if n.Name().Text() == "deleteProperty" && b.reflectionDeletionValue(n.Expression(), maps.Clone(seen)) == 1 {
			return 2
		}
		return b.reflectionPropertyValueLimit(n, seen, b.reflectionDeletionValue, 3)
	case ast.KindElementAccessExpression:
		if !reflectionFunctionMember(n) && b.reflectionDeletionValue(n.Expression(), maps.Clone(seen)) >= 3 {
			return 3
		}
		key := n.AsElementAccessExpression().ArgumentExpression
		if b.reflectionDeletionValue(n.Expression(), maps.Clone(seen)) == 1 {
			if key.Kind == ast.KindStringLiteral {
				if key.Text() == "deleteProperty" {
					return 2
				}
				return b.reflectionPropertyValueLimit(n, seen, b.reflectionDeletionValue, 3)
			}
			return 3
		}
		return b.reflectionPropertyValueLimit(n, seen, b.reflectionDeletionValue, 3)
	}
	return 0
}

// A deleted fixed field cannot retain its CLR layout. Conservatively keep all
// literals containing a potentially deleted name in dynamic storage, including
// aliases passed across functions and modules. Unknown delete-expression keys
// cover every literal. Dynamic reflection uses the target's known field names,
// or covers every literal when an object/any parameter hides those names.
func (b *irBuilder) prepareDeletion(files []*ast.SourceFile) {
	b.prepareReflectionSources(files)
	b.deletedNames = map[string]bool{}
	var visit func(*ast.Node)
	visit = func(n *ast.Node) {
		if ast.IsClassLike(n) {
			b.prepareClassBaseReturns(n)
		}
		if n.Kind == ast.KindConstructor {
			b.prepareConstructorReturns(n)
		}
		if n.Kind == ast.KindCallExpression {
			b.prepareDefinition(n)
			kind := b.reflectionDeletionCallKind(n)
			arguments := b.reflectionDeletionArguments(n)
			if kind >= 3 {
				b.deleteAnyName = true
			}
			if kind >= 2 && len(arguments) == 0 {
				_, method := deletionAccess(n.Expression())
				if method == "apply" {
					b.deleteAnyName = true
				}
			}
			// Binding a target hides it from the eventual invocation. Prepare
			// its layout here, even when the property key is supplied later.
			if len(arguments) == 0 {
				owner, method := deletionAccess(n.Expression())
				bound := n.AsCallExpression().Arguments.Nodes
				if method == "bind" && len(bound) >= 2 && b.reflectionDeletionValue(owner, map[*ast.Node]bool{}) >= 2 {
					arguments = bound[1:]
				}
			}
			if len(arguments) >= 1 {
				var keys []string
				known := false
				if len(arguments) >= 2 {
					keys, known = b.constantDeletionKeys(arguments[1])
				}
				if known {
					for _, key := range keys {
						b.deletedNames[key] = true
					}
				} else {
					properties := b.c.GetPropertiesOfType(b.c.GetTypeAtLocation(arguments[0]))
					if len(properties) == 0 {
						b.deleteAnyName = true
					}
					for _, property := range properties {
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
				if names, known := b.constantDeletionKeys(key); known {
					for _, name := range names {
						b.deletedNames[name] = true
					}
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

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

func (b *irBuilder) isBuiltinIdentifier(n *ast.Node) bool {
	symbol := b.c.GetSymbolAtLocation(n)
	if n.Parent != nil && n.Parent.Kind == ast.KindShorthandPropertyAssignment {
		symbol = b.c.GetShorthandAssignmentValueSymbol(n.Parent)
	}
	if symbol == nil || symbol.ValueDeclaration == nil {
		return false
	}
	file := ast.GetSourceFileOfNode(symbol.ValueDeclaration)
	return file != nil && strings.HasPrefix(file.FileName(), "bundled:///libs/")
}

func identifierWrite(n *ast.Node) bool {
	for n.Parent != nil && n.Parent.Kind == ast.KindParenthesizedExpression {
		n = n.Parent
	}
	parent := n.Parent
	if parent == nil {
		return false
	}
	switch parent.Kind {
	case ast.KindBinaryExpression:
		expression := parent.AsBinaryExpression()
		return expression.Left == n && expression.OperatorToken.Kind >= ast.KindFirstAssignment && expression.OperatorToken.Kind <= ast.KindLastAssignment
	case ast.KindPrefixUnaryExpression:
		operator := parent.AsPrefixUnaryExpression().Operator
		return operator == ast.KindPlusPlusToken || operator == ast.KindMinusMinusToken
	case ast.KindPostfixUnaryExpression:
		return true
	}
	return false
}

func (b *irBuilder) numberPredicate(n *ast.Node) int {
	for n.Kind == ast.KindParenthesizedExpression {
		n = n.Expression()
	}
	var owner *ast.Node
	name := ""
	switch n.Kind {
	case ast.KindPropertyAccessExpression:
		owner, name = n.Expression(), n.Name().Text()
	case ast.KindElementAccessExpression:
		expression := n.AsElementAccessExpression()
		if expression.ArgumentExpression.Kind != ast.KindStringLiteral {
			return -1
		}
		owner, name = expression.Expression, expression.ArgumentExpression.Text()
	default:
		return -1
	}
	if owner.Kind != ast.KindIdentifier || owner.Text() != "Number" || !b.isBuiltinIdentifier(owner) {
		return -1
	}
	return numberPredicateName(name)
}

func numberPredicateName(name string) int {
	switch name {
	case "isNaN":
		return 0
	case "isFinite":
		return 1
	case "isInteger":
		return 2
	case "isSafeInteger":
		return 3
	}
	return -1
}

// A declaration from the unchanged built-in Number object can use its cached
// functions directly. Keep ordinary destructuring for shadowed Number bindings.
func (b *irBuilder) numberPredicateBindings(n *ast.Node) (int, bool) {
	if n.Name().Kind != ast.KindObjectBindingPattern || n.Initializer() == nil {
		return 0, false
	}
	source := n.Initializer()
	for source.Kind == ast.KindParenthesizedExpression {
		source = source.Expression()
	}
	if source.Kind != ast.KindIdentifier || source.Text() != "Number" || !b.isBuiltinIdentifier(source) {
		return 0, false
	}
	block := b.node(12, n, "Number bindings")
	first, last := 0, 0
	for _, element := range n.Name().Elements() {
		binding := element.AsBindingElement()
		if binding.DotDotDotToken != nil {
			b.unsupported(element)
		}
		key := binding.PropertyName
		if key == nil {
			key = element.Name()
		}
		if key.Kind == ast.KindComputedPropertyName {
			key = key.Expression()
			for key.Kind == ast.KindParenthesizedExpression {
				key = key.Expression()
			}
			if key.Kind != ast.KindStringLiteral && key.Kind != ast.KindNoSubstitutionTemplateLiteral {
				b.unsupported(key)
			}
		}
		mode := numberPredicateName(key.Text())
		if mode < 0 {
			b.unsupported(key)
		}
		value := b.node(43+mode, element, "number predicate function")
		b.nodes[value-1].typ = b.typ(b.c.GetAnyType())
		item := 0
		if element.Name().Kind == ast.KindIdentifier {
			item = b.node(13, element, element.Name().Text())
			row := b.nodes[item-1]
			row.name = b.text(element.Name().Text())
			row.symbol = b.symbol(element.Name())
			row.typ = b.typ(b.c.GetTypeAtLocation(element.Name()))
			row.left = value
			if ast.GetCombinedNodeFlags(n)&ast.NodeFlagsConst != 0 {
				row.constant = 1
			}
			if ast.GetCombinedNodeFlags(n)&ast.NodeFlagsBlockScoped == 0 {
				row.fourth = b.node(1, n, "var")
				b.nodes[row.fourth-1].typ = b.typ(b.c.GetStringType())
			}
			b.symbols[row.symbol-1].typ = row.typ
			b.nodes[item-1] = row
		} else {
			item = b.lower(element.Name())
			b.nodes[item-1].left = value
		}
		// These methods are defined, so binding defaults must not execute.
		if first == 0 {
			first = item
		} else {
			b.nodes[last-1].next = item
		}
		last = item
	}
	b.nodes[block-1].children = first
	return block, true
}

// IR void denotes a CLR operation with no stack result, not a TS annotation.
func (b *irBuilder) intrinsicVoidType() int {
	if id := b.primitives[3]; id != 0 {
		return id
	}
	b.types = append(b.types, irType{kind: 3})
	id := len(b.types)
	b.primitives[3] = id
	return id
}
func (b *irBuilder) intrinsicVoidCall(callee int) bool {
	property := b.nodes[callee-1]
	if property.kind != 5 || property.name == 0 || property.left == 0 || b.texts[property.name-1] != "log" {
		return false
	}
	owner := b.nodes[property.left-1]
	return owner.kind == 3 && owner.symbol == 0 && owner.name != 0 && b.texts[owner.name-1] == "console"
}

func (b *irBuilder) objectEnumeration(n *ast.Node) int {
	name := ""
	if n.Kind == ast.KindPropertyAccessExpression {
		name = n.Name().Text()
	} else if n.Kind == ast.KindElementAccessExpression {
		argument := n.AsElementAccessExpression().ArgumentExpression
		if argument.Kind != ast.KindStringLiteral && argument.Kind != ast.KindNoSubstitutionTemplateLiteral {
			return -1
		}
		name = argument.Text()
	} else {
		return -1
	}
	owner := n.Expression()
	for owner.Kind == ast.KindParenthesizedExpression {
		owner = owner.Expression()
	}
	if owner.Kind != ast.KindIdentifier || owner.Text() != "Object" || !b.isBuiltinIdentifier(owner) {
		return -1
	}
	switch name {
	case "keys":
		return 47
	case "values":
		return 48
	case "entries":
		return 49
	}
	return -1
}

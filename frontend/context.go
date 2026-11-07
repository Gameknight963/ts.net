package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
	"strconv"
	"strings"
)

// A fresh literal can adopt its expected storage without changing an existing
// object's identity or weakening that existing object's field contracts.
func (b *irBuilder) contextualObject(n *ast.Node, row *irNode) {
	if n.Kind != ast.KindObjectLiteralExpression {
		return
	}
	// Accessor literals use dynamic storage so their properties remain
	// accessors even when a call supplies a structural contextual type.
	for _, property := range n.AsObjectLiteralExpression().Properties.Nodes {
		if property.Kind == ast.KindGetAccessor || property.Kind == ast.KindSetAccessor {
			return
		}
	}
	// Inferred binding patterns can supply placeholder any fields while their
	// resulting variables still expect the literal's narrower type. Only adopt
	// storage where the consumer has its own established type.
	site := n
	for site != nil && site.Parent != nil {
		if site.Parent.Kind == ast.KindParenthesizedExpression {
			site = site.Parent
		} else if site.Parent.Kind == ast.KindPropertyAssignment {
			site = site.Parent.Parent
		} else {
			break
		}
	}
	if site == nil || site.Parent == nil {
		return
	}
	switch parent := site.Parent; parent.Kind {
	case ast.KindVariableDeclaration, ast.KindParameter:
		if parent.Type() == nil {
			return
		}
	case ast.KindCallExpression, ast.KindAsExpression, ast.KindTypeAssertionExpression:
	case ast.KindArrowFunction:
		if parent.Type() == nil && b.c.GetContextualType(parent, checker.ContextFlagsNone) == nil {
			return
		}
	case ast.KindReturnStatement:
		owner := parent.Parent
		for owner != nil && owner.Kind != ast.KindFunctionDeclaration && owner.Kind != ast.KindFunctionExpression && owner.Kind != ast.KindArrowFunction {
			owner = owner.Parent
		}
		if owner == nil {
			return
		}
		if owner.Type() == nil && (owner.Kind == ast.KindFunctionDeclaration || b.c.GetContextualType(owner, checker.ContextFlagsNone) == nil) {
			return
		}
	default:
		return
	}
	context := b.c.GetContextualType(n, checker.ContextFlagsNone)
	if context == nil {
		return
	}
	target := b.typ(context)
	if b.types[target-1].kind != 6 {
		return
	}
	for property := row.children; property != 0; property = b.nodes[property-1].next {
		if b.nodes[property-1].kind == 35 || b.nodes[property-1].right != 0 {
			// Dynamic entries keep presence separately, including missing
			// contextual optional properties and additional runtime keys.
			row.typ = target
			return
		}
	}

	present := map[int]bool{}
	for property := row.children; property != 0; property = b.nodes[property-1].next {
		present[b.nodes[property-1].name] = true
	}
	var selected []int
	missing := false
	for member := b.types[target-1].fields; member != 0; member = b.fields[member-1].next {
		record := b.fields[member-1]
		if !present[record.name] {
			if record.optional == 0 {
				return
			}
			missing = true
			continue
		}
		selected = append(selected, member)
	}
	if len(selected) != len(present) {
		return
	}
	if !missing {
		row.typ = target
		return
	}

	// Keep absent optional properties out of the physical shape. Existing
	// optional accessors return undefined and add storage only on a later write.
	actual := map[int]int{}
	for member := b.types[row.typ-1].fields; member != 0; member = b.fields[member-1].next {
		record := b.fields[member-1]
		actual[record.name] = record.typ
	}
	unchanged := true
	for _, member := range selected {
		record := b.fields[member-1]
		if actual[record.name] != record.typ {
			unchanged = false
			break
		}
	}
	if unchanged {
		return
	}

	var key strings.Builder
	key.WriteString(strconv.Itoa(target))
	for _, member := range selected {
		key.WriteByte(':')
		key.WriteString(strconv.Itoa(member))
	}
	if b.contextualShapes == nil {
		b.contextualShapes = map[string]int{}
	}
	if cached := b.contextualShapes[key.String()]; cached != 0 {
		row.typ = cached
		return
	}
	b.types = append(b.types, irType{kind: 6})
	shape := len(b.types)
	last := 0
	for _, member := range selected {
		record := b.fields[member-1]
		b.fields = append(b.fields, irField{name: record.name, typ: record.typ})
		field := len(b.fields)
		if last == 0 {
			b.types[shape-1].fields = field
		} else {
			b.fields[last-1].next = field
		}
		last = field
	}
	b.contextualShapes[key.String()] = shape
	row.typ = shape
}

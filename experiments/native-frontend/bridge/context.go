package main

import (
	"github.com/microsoft/typescript-go/internal/ast"
	"github.com/microsoft/typescript-go/internal/checker"
)

// A fresh literal can adopt its expected storage without changing an existing
// object's identity or weakening that existing object's field contracts.
func (b *irBuilder) contextualObject(n *ast.Node, row *irNode) {
	if n.Kind != ast.KindObjectLiteralExpression {
		return
	}
	// Inferred binding patterns can supply placeholder any fields while their
	// resulting variables still expect the literal's narrower type. Only adopt
	// storage where the consumer has its own established type.
	site := n
	for site.Parent != nil && site.Parent.Kind == ast.KindPropertyAssignment {
		site = site.Parent.Parent
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
	case ast.KindReturnStatement:
		owner := parent.Parent
		for owner != nil && owner.Kind != ast.KindFunctionDeclaration {
			owner = owner.Parent
		}
		if owner == nil || owner.Type() == nil {
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
	count := 0
	for member := b.types[target-1].fields; member != 0; member = b.fields[member-1].next {
		name := b.fields[member-1].name
		found := false
		for property := row.children; property != 0; property = b.nodes[property-1].next {
			if b.nodes[property-1].name == name {
				found = true
				break
			}
		}
		if !found {
			return
		}
		count++
	}
	properties := 0
	for property := row.children; property != 0; property = b.nodes[property-1].next {
		properties++
	}
	if count == properties {
		row.typ = target
	}
}

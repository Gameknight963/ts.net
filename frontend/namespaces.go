package main

// Intrinsic calls remain available when their namespace never becomes a value.
// Once it does, property lookups must observe writes through aliases as well.
func (b *irBuilder) dynamicNamespaces() {
	number, object := false, false
	for _, node := range b.nodes {
		if node.kind == 66 {
			name := b.texts[b.tokens[node.token-1].text-1]
			number = number || name == "Number"
			object = object || name == "Object"
		}
	}
	if !number && !object {
		return
	}
	count := len(b.nodes)
	namespace := func(name string, token int) int {
		original := b.tokens[token-1]
		b.tokens = append(b.tokens, irToken{b.text(name), original.line, original.column})
		b.nodes = append(b.nodes, irNode{kind: 66, token: len(b.tokens), typ: b.typ(b.c.GetAnyType())})
		return len(b.nodes)
	}
	property := func(name, owner string, token int) int {
		target := namespace(owner, token)
		b.nodes = append(b.nodes, irNode{kind: 5, token: token, name: b.text(name), left: target, typ: b.typ(b.c.GetAnyType())})
		return len(b.nodes)
	}
	predicates := []string{"isNaN", "isFinite", "isInteger", "isSafeInteger"}
	objects := []string{"keys", "values", "entries"}
	for index := 0; index < count; index++ {
		node := b.nodes[index]
		if number && node.kind >= 39 && node.kind <= 42 {
			node.left = property(predicates[node.kind-39], "Number", node.token)
			node.kind = 6
			b.nodes[index] = node
		} else if number && node.kind >= 43 && node.kind <= 46 {
			name := predicates[node.kind-43]
			node.kind = 5
			node.name = b.text(name)
			node.left = namespace("Number", node.token)
			b.nodes[index] = node
		} else if object && node.kind >= 47 && node.kind <= 49 {
			name := objects[node.kind-47]
			node.kind = 5
			node.name = b.text(name)
			node.left = namespace("Object", node.token)
			b.nodes[index] = node
		} else if object && node.kind == 3 && node.symbol == 0 && node.name != 0 && b.texts[node.name-1] == "Object" {
			node.kind = 66
			node.typ = b.typ(b.c.GetAnyType())
			b.nodes[index] = node
		}
	}
}

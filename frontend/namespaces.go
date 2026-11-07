package main

// Intrinsic calls remain available when their namespace never becomes a value.
// Once it does, property lookups must observe writes through aliases as well.
func (b *irBuilder) dynamicNamespaces() {
	number, object := false, false
	// Definitions and plain assignment targets do not read a constructor alias.
	noRead := map[int]bool{}
	for _, node := range b.nodes {
		if node.kind == 4 {
			for child := node.children; child != 0; child = b.nodes[child-1].next {
				noRead[child] = true
			}
		}
		if node.kind == 10 && b.texts[b.tokens[node.token-1].text-1] == "=" {
			noRead[node.left] = true
		}
	}
	for index, node := range b.nodes {
		if !noRead[index+1] {
			if (node.kind == 5 || node.kind == 63) && node.name != 0 && b.texts[node.name-1] == "constructor" {
				object = true
			}
			if (node.kind == 33 || node.kind == 64) && node.right != 0 {
				key := b.nodes[node.right-1]
				if key.kind == 1 {
					object = object || b.texts[b.tokens[key.token-1].text-1] == "constructor"
				} else if key.typ == 0 || (b.types[key.typ-1].kind != 0 && b.types[key.typ-1].kind != 2) {
					object = true
				}
			}
		}

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
		b.tokens = append(b.tokens, irToken{b.text(name), original.line, original.column, original.file})
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

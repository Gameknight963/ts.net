package main

func errorKind(name string) bool {
	switch name {
	case "Error", "TypeError", "RangeError", "ReferenceError", "SyntaxError", "URIError", "EvalError", "AggregateError", "SuppressedError":
		return true
	}
	return false
}

func (b *irBuilder) catchBinding(id int) {
	if id == 0 {
		return
	}
	kind := b.nodes[id-1].kind
	if kind == 13 {
		b.nodes[id-1].fourth = 0
	}
	if kind == 37 {
		b.nodes[id-1].name = b.text("let")
		b.catchBinding(b.nodes[id-1].right)
	}
	if kind == 36 {
		for child := b.nodes[id-1].children; child != 0; child = b.nodes[child-1].next {
			b.catchBinding(child)
		}
	}
}

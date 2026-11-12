package main

// Small tails without declarations can run in either branch without introducing
// new bindings. Keep larger tails and scopes in the shared continuation.
func (a *asyncExpressionLowering) copyBranchTail(root int) int {
	seen := map[int]bool{}
	var eligible func(int) bool
	eligible = func(id int) bool {
		if id == 0 || seen[id] {
			return true
		}
		seen[id] = true
		if len(seen) > 40 {
			return false
		}
		n := a.b.nodes[id-1]
		switch n.kind {
		case 0, 1, 2, 3, 6, 7, 8, 9, 10, 11, 14, 18, 20, 21, 22, 23, 30, 31, 33, 39, 40, 41, 42:
		case 12:
			if n.name != 0 {
				return false
			}
		default:
			return false
		}
		for _, child := range []int{n.left, n.right, n.third, n.fourth, n.children, n.next} {
			if !eligible(child) {
				return false
			}
		}
		return true
	}
	if !eligible(root) {
		return 0
	}
	copies := map[int]int{}
	var copy func(int) int
	copy = func(id int) int {
		if id == 0 {
			return 0
		}
		if cloned := copies[id]; cloned != 0 {
			return cloned
		}
		n := a.b.nodes[id-1]
		a.b.nodes = append(a.b.nodes, n)
		cloned := len(a.b.nodes)
		copies[id] = cloned
		n.left, n.right, n.third, n.fourth = copy(n.left), copy(n.right), copy(n.third), copy(n.fourth)
		n.children, n.next = copy(n.children), copy(n.next)
		a.b.nodes[cloned-1] = n
		return cloned
	}
	return copy(root)
}

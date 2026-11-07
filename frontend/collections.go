package main

func collectionType(name string) bool {
	switch name {
	case "RegExpStringIterator", "WeakMap", "WeakSet", "WeakMapConstructor", "WeakSetConstructor", "Map", "ReadonlyMap", "MapConstructor", "MapIterator", "Set", "ReadonlySet", "SetConstructor", "SetIterator", "IteratorYieldResult", "IteratorReturnResult", "IterableIterator", "IteratorObject":
		return true
	}
	return false
}

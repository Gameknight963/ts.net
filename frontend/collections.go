package main

func collectionType(name string) bool {
	switch name {
	case "Map", "ReadonlyMap", "MapConstructor", "MapIterator", "Set", "ReadonlySet", "SetConstructor", "SetIterator", "IteratorYieldResult", "IteratorReturnResult", "IterableIterator", "IteratorObject":
		return true
	}
	return false
}

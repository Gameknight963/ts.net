package main

func collectionType(name string) bool {
	switch name {
	case "URLSearchParamsIterator", "AsyncGenerator", "AsyncIterator", "AsyncIterableIterator", "AsyncIteratorObject", "Generator", "GeneratorFunction", "GeneratorFunctionConstructor", "Iterator", "IteratorConstructor", "ArrayIterator", "StringIterator", "RegExpStringIterator", "WeakMap", "WeakSet", "WeakMapConstructor", "WeakSetConstructor", "Map", "ReadonlyMap", "MapConstructor", "MapIterator", "Set", "ReadonlySet", "SetConstructor", "SetIterator", "IteratorYieldResult", "IteratorReturnResult", "IterableIterator", "IteratorObject":
		return true
	}
	return false
}

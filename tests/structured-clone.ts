const shared = { value: 7 };
const graph: any = { first: shared, second: shared };
graph.self = graph;
const copy = structuredClone(graph);
copy.first.value = 9;
console.log(copy !== graph, copy.self === copy, copy.first === copy.second, graph.first.value, copy.second.value);
const typed = structuredClone({ name: 'test', numbers: [1, 2, 3], child: { ok: true } });
console.log(typed.name, typed.numbers.reduce((a, b) => a + b, 0), typed.child.ok);
const sparse: any[] = new Array(5); sparse[2] = shared; (sparse as any).note = shared;
const sparseCopy = structuredClone(sparse);
console.log(sparseCopy.length, 0 in sparseCopy, 2 in sparseCopy, sparseCopy[2] === (sparseCopy as any).note);
const map = new Map<any, any>(); const set = new Set<any>();
map.set(shared, set); map.set(map, shared); set.add(shared); set.add(map);
const collections = structuredClone({ map, set, shared });
console.log(collections.map.get(collections.shared) === collections.set, collections.map.get(collections.map) === collections.shared, collections.set.has(collections.map));
const buffer = new ArrayBuffer(16); const bytes = new Uint8Array(buffer, 2, 8); bytes[0] = 42;
const binary = structuredClone({ buffer, bytes, view: new DataView(buffer, 4, 6) });
binary.bytes[0] = 21;
console.log(binary.buffer !== buffer, binary.bytes.buffer === binary.buffer, binary.view.buffer === binary.buffer, binary.bytes.byteOffset, binary.view.byteLength, bytes[0], binary.bytes[0]);
const special = structuredClone({ date: new Date(12345), invalid: new Date(NaN), regex: /ab/gi, number: new Number(4), text: new String('hi'), boolean: new Boolean(false), big: 123n });
console.log(special.date.getTime(), Number.isNaN(special.invalid.getTime()), special.regex.source, special.regex.flags, special.number.valueOf(), special.text.valueOf(), special.boolean.valueOf(), special.big.toString());
const regex = /a/g; regex.lastIndex = 3; console.log(structuredClone(regex).lastIndex);
class Item { value = 8; method() { return 9; } }
const item: any = structuredClone(new Item()); console.log(item.value, item instanceof Item, typeof item.method);
let reads = 0; const accessor = { get value() { reads++; return shared; } };
const accessorCopy = structuredClone(accessor); console.log(reads, accessorCopy.value.value, typeof Object.getOwnPropertyDescriptor(accessorCopy, 'value')!.get);
for (const value of [Symbol('x'), () => 1, new WeakMap(), Promise.resolve(1), { callback: () => 1 }]) {
    try { structuredClone(value); console.log('missed'); } catch (error) { console.log((error as Error).name, (error as DOMException).code); }
}
console.log(structuredClone(undefined), structuredClone(null), Number.isNaN(structuredClone(NaN)), Object.is(structuredClone(-0), -0));
console.log(structuredClone({ x: 3 }, { transfer: [] }).x);
const blob = structuredClone(new Blob(['hello'], { type: 'text/plain' }));
console.log(blob.size, blob.type); blob.text().then(text => console.log(text));

const error: any = new TypeError('oops', { cause: { code: 3 } });
error.cause.owner = error;
const errorCopy = structuredClone(error);
console.log(errorCopy instanceof TypeError, errorCopy.message, errorCopy.cause.code, errorCopy.cause.owner === errorCopy, errorCopy.stack === error.stack);
const dom = structuredClone(new DOMException('cancelled', 'AbortError'));
console.log(dom instanceof DOMException, dom.name, dom.message, dom.code);
const file = structuredClone(new File(['file'], 'a.txt', { type: 'text/plain', lastModified: 123 }));
console.log(file instanceof File, file.name, file.size, file.type, file.lastModified);
const symbol = Symbol('ignored'); const symbols: any = { x: 4 }; symbols[symbol] = () => 1;
console.log(Object.getOwnPropertySymbols(structuredClone(symbols)).length);

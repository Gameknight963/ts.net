export {};
import { source, sourceAlias, replaceable, replace } from './fixed-object-alias-source.js';
type Item = { count: number };
function read(item: Item): number { return item.count; }
function forward(callback: (item: Item) => number, item: Item): number { return callback(item); }
const original = { count: 7 };
const first = original;
const second = first;
function readAliases(): number { return read(first) + forward(read, second); }
console.log('aliases', readAliases(), first === second, second === original);
original.count = 11;
console.log('updated', readAliases());
let mutable = { count: 13 };
const snapshot = mutable;
function readSnapshot(): number { return read(snapshot); }
mutable = { count: 17 };
console.log('snapshot', readSnapshot(), read(mutable), snapshot === mutable);
const dynamic = new Proxy({ count: 19 }, { get(target, key, receiver) { return Reflect.get(target, key, receiver); } });
const dynamicAlias = dynamic;
function readDynamic(): number { return read(dynamicAlias); }
console.log('proxy alias', readDynamic());
function captured(): () => number {
    const object = { count: 23 };
    const alias = object;
    return () => read(alias);
}
console.log('captured alias', captured()());
let undefinedFirst: Item;
const undefinedSnapshot = undefinedFirst!;
undefinedFirst = { count: 29 };
function inspectUndefined(): boolean { return undefinedSnapshot === undefined; }
console.log('undefined alias', inspectUndefined());
function early(): number { return read(laterAlias); }
try { early(); } catch (error) { console.log('tdz', (error as Error).name); }
const laterAlias = second;
console.log('initialized alias', early());
const importedAlias = sourceAlias;
function readImported(): number { return read(importedAlias); }
console.log('imported alias', readImported(), importedAlias === source);
const importedSnapshot = replaceable;
replace();
function readImportedSnapshot(): number { return read(importedSnapshot); }
console.log('imported snapshot', readImportedSnapshot(), read(replaceable));

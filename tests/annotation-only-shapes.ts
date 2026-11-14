export {};
interface Entry { count: number; label: string; }
function read(value: Entry): string { return value.label + ":" + value.count; }
function copy(value: Entry): Entry { return { ...value }; }
function assign(value: Entry): Entry { return Object.assign({}, value); }
function generic<T extends Entry>(value: T): string { return read(value); }
const parsed: Entry = JSON.parse('{"count":7,"label":"parsed"}');
console.log("parsed", read(parsed), generic(parsed), read(copy(parsed)), read(assign(parsed)));
const dynamic: any = Object.create(null);
dynamic.count = 11;
dynamic.label = "dynamic";
console.log("dynamic", read(dynamic), read(copy(dynamic)), read(assign(dynamic)));
let getterCalls = 0;
Object.defineProperty(dynamic, "count", { configurable: true, enumerable: true, get() { getterCalls++; return 13; } });
console.log("getter", read(dynamic), read(copy(dynamic)), read(assign(dynamic)), getterCalls);
interface Nested { entry: Entry; }
function nested(value: Nested): string { return read(value.entry); }
const outer: Nested = JSON.parse('{"entry":{"count":17,"label":"nested"}}');
console.log("nested", nested(outer));
class Holder {
    constructor(public value: Entry) {}
    read(): string { return read(this.value); }
}
console.log("class field", new Holder(parsed).read());
function optional(value: { count?: number }): number | undefined { return value.count; }
console.log("optional", optional(JSON.parse('{}')), optional(JSON.parse('{"count":19}')));

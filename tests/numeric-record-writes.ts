export {};
function write(target: {value: number}, value: number): void {target.value = value;}
function create() {return {value: 1, label: 'record'};}
const target = create();
for (const value of [2, -0, NaN, Infinity, undefined]) {
    write(target, value as number);
    console.log('value', target.value, target.value === 0 ? 1 / target.value : 0);
}
for (const operation of [Object.freeze, Object.seal, Object.preventExtensions]) {
    const item = create(); operation(item);
    try {write(item, 2); console.log('write', item.value);} catch {console.log('readonly', item.value);}
}
const accessor = create();
let stored = 0, reads = 0, writes = 0;
Object.defineProperty(accessor, 'value', {get() {reads++; return stored;}, set(value: number) {writes++; stored = value;}, configurable: true});
write(accessor, 7);
console.log('accessor', accessor.value, reads, writes);
const readonly = create();
Object.defineProperty(readonly, 'value', {writable: false});
try {write(readonly, 3);} catch {console.log('readonly descriptor', readonly.value);}
const removed = create();
delete (removed as any).value;
Object.setPrototypeOf(removed, {set value(value: number) {console.log('inherited setter', value);}});
write(removed, 4);
console.log('removed', Object.hasOwn(removed, 'value'));
const proxy = new Proxy(create(), {set(object, key, value, receiver) {
    console.log('proxy', String(key), value); return Reflect.set(object, key, value, receiver);
}});
write(proxy, 5);
console.log('proxy result', proxy.value);
const spread = {...create()};
delete (spread as any).value;
write(spread, 6);
console.log('spread', spread.value, Object.hasOwn(spread, 'value'));

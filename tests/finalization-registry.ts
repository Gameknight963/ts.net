/// <reference lib="es2023.collection" />
export {};
const registry = new FinalizationRegistry<unknown>(() => {});
const target = {}, token = {};
console.log('register', registry.register(target, 'held', token) === undefined);
registry.register({}, 'second', token);
console.log('unregister', registry.unregister(token), registry.unregister(token));
for (const value of [undefined, null, 1, 'text', Symbol.for('registered')]) {
    try {registry.register(value as any, 'held'); console.log('unexpected');} catch (error) {console.log('target', (error as Error).name);}
    try {registry.unregister(value as any); console.log('unexpected');} catch (error) {console.log('token', (error as Error).name);}
}
try {registry.register(target, target);} catch (error) {console.log('same', (error as Error).name);}
try {registry.register(target, 1, null as any);} catch (error) {console.log('register token', (error as Error).name);}
const symbol = Symbol('target'), symbolToken = Symbol('token');
registry.register(symbol, 'symbol', symbolToken);
console.log('symbols', registry.unregister(symbolToken));
for (const callback of [undefined, null, 1, {}]) try {new FinalizationRegistry(callback as any);} catch (error) {console.log('callback', (error as Error).name);}
try {(FinalizationRegistry as any)(() => {});} catch (error) {console.log('new', (error as Error).name);}
class Derived extends FinalizationRegistry<string> {}
const derived = new Derived(() => {});
Object.freeze(derived);
derived.register(target, 'derived', token);
console.log('derived', derived instanceof Derived, derived instanceof FinalizationRegistry, Object.isFrozen(derived), derived.unregister(token));
const prototype = FinalizationRegistry.prototype;
const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
console.log('metadata', FinalizationRegistry.name, FinalizationRegistry.length, prototype.register.length, prototype.unregister.length, tag.value, tag.writable, tag.enumerable, tag.configurable);
try {prototype.register.call({} as any, target, 1);} catch (error) {console.log('receiver', (error as Error).name);}
console.log('global', globalThis.FinalizationRegistry === FinalizationRegistry);

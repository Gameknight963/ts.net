export {};
function Alternate() {}
const bound = Alternate.bind(null);
let reads = 0;
Object.defineProperty(bound, 'prototype', {get() {reads++; return Alternate.prototype;}, configurable: true});
for (const callback of [undefined, null, 1, {}]) {
    reads = 0;
    try {Reflect.construct(FinalizationRegistry, [callback], bound); console.log('unexpected');}
    catch (error) {console.log('invalid', (error as Error).name, reads);}
}
reads = 0;
const registry = Reflect.construct(FinalizationRegistry, [() => {}], bound);
console.log('prototype', reads, Object.getPrototypeOf(registry) === Alternate.prototype);
const token = {};
FinalizationRegistry.prototype.register.call(registry, {}, 'held', token);
console.log('slots', FinalizationRegistry.prototype.unregister.call(registry, token));
const fallback: any = function Fallback() {};
fallback.prototype = 1;
const normal = Reflect.construct(FinalizationRegistry, [() => {}], fallback);
console.log('fallback', Object.getPrototypeOf(normal) === FinalizationRegistry.prototype);

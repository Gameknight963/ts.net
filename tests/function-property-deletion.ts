function sample(a: number, b: number): number {return a + b;}
const fn: any = sample;
const prototype: any = Object.getPrototypeOf(fn);
console.log('prototype', typeof prototype, prototype.name, prototype.length, prototype(), Object.getPrototypeOf(prototype) === Object.prototype);
console.log('methods', Object.hasOwn(prototype, 'call'), Object.hasOwn(prototype, 'apply'), Object.hasOwn(prototype, 'bind'), Object.keys(prototype).length);
console.log('before', fn.name, fn.length, Object.hasOwn(fn, 'name'), Object.hasOwn(fn, 'length'));
console.log('deleted', delete fn.name, delete fn.length, fn.name, fn.length, Object.hasOwn(fn, 'name'), Object.hasOwn(fn, 'length'));
try {fn.name = 'changed';} catch (error) {console.log('inherited readonly', (error as Error).name);}
console.log('call', fn.call(null, 1, 2), fn.apply(null, [3, 4]), fn.bind(null, 5)(6));
try {console.log(delete fn.prototype);} catch (error) {console.log('prototype locked', (error as Error).name);}
Object.setPrototypeOf(fn, {});
fn.name = 'changed'; fn.length = 9;
console.log('replaced', fn.name, fn.length, Object.keys(fn).join(','));
console.log('again', delete fn.name, delete fn.length, fn.name, fn.length, typeof fn.call);
Object.setPrototypeOf(fn, prototype);
const bound: any = sample.bind(null, 1);
console.log('bound', bound.length, delete bound.length, bound.length, delete bound.name, bound.name);
const arrow: any = (value: number) => value;
console.log('arrow', delete arrow.prototype, delete arrow.name, arrow.name);
console.log('intrinsic', delete (Object.keys as any).name, (Object.keys as any).name);
const saved = prototype.call;
const removed = delete prototype.call; const missing = typeof bound.call;
prototype.call = saved;
console.log('delete method', removed, missing);
console.log('restore method', typeof bound.call, Object.keys(prototype).join(','));
const metadataKey: string = 'name';
class Metadata {static [metadataKey] = 21;}
const metadata: any = Metadata;
console.log('class metadata', metadata.name);
metadata.name = 22;
console.log('class overwrite', metadata.name, delete metadata.name, metadata.name);
class Methods {static method(): void {} method(): void {}}
console.log('class methods', delete (Methods as any).method, delete (Methods.prototype as any).method);
(Methods as any).method = 1; (Methods.prototype as any).method = 2;
console.log('class enumerable', Object.keys(Methods).join(','), Object.keys(Methods.prototype).join(','));
const custom: any = () => 0;
custom.tag = 27;
Object.setPrototypeOf(custom, {get name(): number {return (this as any).tag;}, get length(): number {return (this as any).tag + 1;}});
console.log('custom prototype', delete custom.name, delete custom.length, custom.name, custom.length, typeof custom.call);
function Holder(): void {}
const holderPrototype: any = Holder.prototype;
console.log('constructor deletion', delete holderPrototype.constructor);
holderPrototype.constructor = Holder;
console.log('constructor enumerable', Object.keys(holderPrototype).join(','));

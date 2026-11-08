export {};
function example(a: number, b: number) {return a + b;}
Object.defineProperty(example, 'name', {value: 'renamed', writable: true});
(example as any).name = 'changed';
Object.defineProperty(example, 'length', {get() {return 8;}, configurable: true});
console.log('metadata', example.name, example.length, example(2, 3));
const bound = example.bind(null, 1);
console.log('bound', bound.name, bound.length, bound(4));
Object.defineProperty(bound, 'length', {value: 2});
console.log('bound redefined', bound.length);
function Constructor(this: any) {this.value = 7;}
const prototype = {marker: 9};
Object.defineProperty(Constructor, 'prototype', {value: prototype, writable: false});
const instance = new (Constructor as any)();
console.log('construction', instance.value, instance.marker, instance instanceof Constructor, Object.getPrototypeOf(instance) === prototype);
try {Constructor.prototype = {};} catch (error) {console.log('prototype readonly', (error as Error).name);}
console.log('prototype reject', Reflect.defineProperty(Constructor, 'prototype', {value: {}}));
Object.freeze(example);
console.log('freeze', Object.isFrozen(example), Reflect.defineProperty(example, 'extra', {value: 1}), Reflect.deleteProperty(example, 'name'));
console.log('descriptor', Object.getOwnPropertyDescriptor(example, 'length')!.configurable, Object.getOwnPropertyNames(example).join(','));
function Holder() {}
Object.defineProperty(Holder.prototype, 'constructor', {enumerable: true});
console.log('prototype keys', Object.keys(Holder.prototype).join(','));
Object.freeze(Holder.prototype);
console.log('prototype freeze', Object.isFrozen(Holder.prototype), Reflect.deleteProperty(Holder.prototype, 'constructor'));

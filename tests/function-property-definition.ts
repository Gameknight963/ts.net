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

{
    function ordered(value: number): number {return value;}
    const callable: any = ordered;
    callable.extra = 7;
    callable[1] = 'index';
    Object.defineProperty(callable, 'name', {value: 'changed', writable: true, enumerable: true, configurable: true});
    Object.defineProperty(callable, 'length', {value: 2, writable: true, enumerable: true, configurable: true});
    console.log('function metadata key order', Object.keys(callable).join(','), Object.getOwnPropertyNames(callable).join(','));
    let keys = ''; for (const key in callable) keys += key + ',';
    console.log('function metadata enumeration', keys);
    delete callable.name;
    Object.defineProperty(callable, 'name', {value: 'again', writable: true, enumerable: true, configurable: true});
    console.log('function metadata reinsertion', Object.keys(callable).join(','), Object.getOwnPropertyNames(callable).join(','));
}

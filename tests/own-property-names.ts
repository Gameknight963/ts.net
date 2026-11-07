function names(value: any): string {return Object.getOwnPropertyNames(value).join(',');}
const record = Object.create({inherited: 1}, {hidden: {value: 2}, visible: {value: 3, enumerable: true}, accessor: {get(): never {throw 'getter must not run';}}});
console.log('descriptors', names(record), Object.keys(record).join(','));
const array: any = [1, , 3]; array.extra = 4; array['9'] = 5;
console.log('array', names(array));
console.log('primitives', names('abc'), names(2), names(true));
const boxed: any = Object('ab'); boxed.extra = 1;
console.log('boxed string', names(boxed));
function sample(a: number, b: number): void {}
const fn: any = sample; fn.extra = 1; fn['3'] = 2;
console.log('function', names(fn));
console.log('deleted', delete fn.name, names(fn));
Object.setPrototypeOf(fn, {}); fn.name = 'recreated';
console.log('recreated', names(fn));
const arrow: any = () => 1; arrow.extra = 2; arrow.prototype = {};
console.log('arrow', names(arrow));
const key: string = 'name';
class Example {static [key] = 'replacement'; static extra = 1; static method(): void {} field = 2; method(): void {}}
console.log('class', names(Example), names(Example.prototype), names(new Example()));
function argumentsNames(first: number, second: number): void {(arguments as any).extra = 3; console.log('arguments', names(arguments));}
argumentsNames(1, 2);
const error: any = new Error('message', {cause: 4}); error.extra = 5;
console.log('error', names(error));
console.log('hidden natives', Object.getOwnPropertyNames(Math).includes('PI'), Object.getOwnPropertyNames(Array.prototype).includes('push'), Object.getOwnPropertyNames(Promise.prototype).includes('then'));
for (const value of [null, undefined]) {try {Object.getOwnPropertyNames(value);} catch(error) {console.log('invalid', (error as Error).name);}}
console.log('metadata', Object.getOwnPropertyNames.name, Object.getOwnPropertyNames.length, Object.keys(Object).includes('getOwnPropertyNames'));

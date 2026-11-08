export {};
const closed = [1, , 3];
console.log('prevent identity', Object.preventExtensions(closed) === closed, Object.isExtensible(closed));
closed[0] = 4;
try {closed[1] = 2;} catch (error) {console.log('fill hole', (error as Error).name);}
try {closed.push(4);} catch (error) {console.log('push', (error as Error).name);}
closed.length = 5;
console.log('grow length', closed.length, Object.keys(closed).join(','), Reflect.defineProperty(closed, '3', {value: 5}));
console.log('delete', Reflect.deleteProperty(closed, '0'), Reflect.set(closed, '0', 8));
const sealed = Object.seal([1, 2]);
sealed[0] = 3;
console.log('sealed', Object.isSealed(sealed), Object.isFrozen(sealed), sealed.join(','), Reflect.deleteProperty(sealed, '0'));
try {sealed.pop();} catch (error) {console.log('sealed pop', (error as Error).name, sealed.length);}
const frozen = Object.freeze([1, 2]);
console.log('frozen', Object.isFrozen(frozen), frozen.join(','), Reflect.set(frozen, '0', 3), Object.getOwnPropertyDescriptor(frozen, 'length')!.writable);
try {(frozen as number[])[0] = 3;} catch (error) {console.log('frozen write', (error as Error).name);}
console.log('frozen copy', frozen.slice().join(','), [...frozen].join(','));
const mixed: any[] = [1, 'two'];
Object.freeze(mixed);
try {mixed[0] = 4;} catch (error) {console.log('mixed frozen', (error as Error).name, mixed.join(','));}
const sparse: any[] = [, 'one'];
Object.preventExtensions(sparse);
try {sparse.reverse();} catch (error) {console.log('reverse hole', (error as Error).name, sparse.join(','));}
const objects = [{value: 1}];
Object.freeze(objects);
objects[0].value = 9;
console.log('shallow', objects[0].value, Object.isFrozen(objects));
let stored = 1;
const accessor: any[] = [0];
Object.defineProperty(accessor, '0', {get() {return stored;}, set(value: number) {stored = value;}, configurable: true});
Object.freeze(accessor);
accessor[0] = 7;
console.log('frozen setter', accessor[0], Object.isFrozen(accessor));
const redefining: any[] = [1];
Object.defineProperty(redefining, '0', {get() {return 2;}, configurable: true});
Object.preventExtensions(redefining);
Object.defineProperty(redefining, '0', {value: 3, writable: true, enumerable: true, configurable: true});
console.log('redefine existing', redefining[0], JSON.stringify(Object.getOwnPropertyDescriptor(redefining, '0')));
const shrinking = Object.seal([1, 2, ,]);
console.log('sealed hole pop', shrinking.pop(), shrinking.length);
console.log('sealed shrink', Reflect.defineProperty(shrinking, 'length', {value: 0}), shrinking.length, shrinking.join(','));
try {shrinking.fill(8, 2);} catch (error) {console.log('sealed fill', (error as Error).name);}
const changing = Object.seal([1, 2]);
Object.defineProperty(changing, '0', {writable: false});
console.log('sealed readonly', Reflect.set(changing, '0', 9), Reflect.set(changing, '1', 7), changing.join(','));
const source = Object.freeze([1, , 3]);
console.log('frozen sparse copy', source.slice().join(','), Object.hasOwn(source.slice(), '1'));
try {(source as any).fill(4);} catch (error) {console.log('frozen fill', (error as Error).name);}
try {(source as any).copyWithin(1, 0);} catch (error) {console.log('frozen copyWithin', (error as Error).name);}
const empty = Object.freeze([]);
console.log('empty frozen', Object.isFrozen(empty), empty.length);
const parent = Object.getPrototypeOf(closed);
console.log('prototype', Reflect.setPrototypeOf(closed, parent), Reflect.setPrototypeOf(closed, {}));
const loop = Object.freeze([1, 2, 3]);
let total = 0;
for (let i = 0; i < loop.length; i++) total += loop[i];
try {for (let i = 0; i < loop.length; i++) (loop as number[])[i] = i;} catch (error) {console.log('frozen numeric loop', total, (error as Error).name, loop.join(','));}
const references = Object.freeze([{value: 1}, {value: 2}]);
try {for (let i = 0; i < references.length; i++) (references as any)[i] = {value: 9};} catch (error) {console.log('frozen reference loop', (error as Error).name, references[0].value);}
let inheritedSetters = 0;
const inheritedParent = Object.create(Array.prototype, {
    '1': {get() {return 11;}, set(value: number) {inheritedSetters += value;}, configurable: true},
    '2': {get() {return 12;}, set(value: number) {inheritedSetters += value;}, configurable: true}
});
const inherited = [0, ,];
Object.setPrototypeOf(inherited, inheritedParent);
Object.preventExtensions(inherited);
inherited[1] = 7;
console.log('inherited setter', inheritedSetters, inherited.length, Object.hasOwn(inherited, '1'));
console.log('inherited push', inherited.push(8), inheritedSetters, Object.hasOwn(inherited, '2'));
let resultSetters = 0;
function resultArray(): any[] {
    const value: any[] = [0, 0];
    Object.defineProperty(value, '0', {get() {return 99;}, set() {resultSetters++;}, configurable: true});
    return Object.preventExtensions(value);
}
const mapResult = resultArray();
const mapping = [1, 2];
(mapping as any).constructor = {[Symbol.species]: function() {return mapResult;}};
console.log('species replace', mapping.map(value => value * 2) === mapResult, mapResult.join(','), resultSetters);
const ofResult = resultArray();
function Output(): any {return ofResult;}
console.log('of replace', (Array.of as any).call(Output, 5, 6) === ofResult, ofResult.join(','), resultSetters);

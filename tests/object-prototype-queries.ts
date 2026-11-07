const ancestor = {a: 1};
const child = Object.create(ancestor, {visible: {value: 2, enumerable: true}, hidden: {value: 3}});
const descendant = Object.create(child);
console.log('prototype', ancestor.isPrototypeOf(child), ancestor.isPrototypeOf(descendant), child.isPrototypeOf(child), Object.prototype.isPrototypeOf(child));
console.log('own', child.hasOwnProperty('visible'), child.hasOwnProperty('hidden'), child.hasOwnProperty('a'));
console.log('enumerable', child.propertyIsEnumerable('visible'), child.propertyIsEnumerable('hidden'), child.propertyIsEnumerable('a'));
console.log('array', [1].propertyIsEnumerable('0'), [1].propertyIsEnumerable('length'));
console.log('primitive', Object.prototype.hasOwnProperty.call('abc', '1'), Object.prototype.propertyIsEnumerable.call('abc', '1'), Object.prototype.propertyIsEnumerable.call('abc', 'length'));
console.log('null prototype', Object.prototype.isPrototypeOf(Object.create(null)));
for (const value of [1, null, undefined, 'text']) console.log('non-object', Object.prototype.isPrototypeOf.call(null, value as any));
try {Object.prototype.isPrototypeOf.call(null, {});} catch (error) {console.log('invalid receiver', (error as Error).name);}
for (const method of [Object.prototype.hasOwnProperty, Object.prototype.propertyIsEnumerable]) {
    try {method.call(null, {toString(): string {console.log('key first'); return 'x';}} as any);} catch(error) {console.log('null receiver', (error as Error).name);}
}
const accessor = {get x(): never {throw 'getter must not run';}};
console.log('accessor', accessor.hasOwnProperty('x'), accessor.propertyIsEnumerable('x'));
const dictionary = Object.create(null); dictionary.hasOwnProperty = 4;
console.log('borrowed', Object.prototype.hasOwnProperty.call(dictionary, 'hasOwnProperty'));
for (const method of [Object.prototype.isPrototypeOf, Object.prototype.hasOwnProperty, Object.prototype.propertyIsEnumerable]) console.log('metadata', method.name, method.length, Object.prototype.propertyIsEnumerable.call(Object.prototype, method.name));

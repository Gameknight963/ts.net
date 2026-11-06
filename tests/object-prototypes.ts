const ancestor = {value: 3};
const target: any = {own: 7};
console.log('get', Object.getPrototypeOf(target) === Object.prototype, Object.getPrototypeOf(Object.prototype));
console.log('set', Object.setPrototypeOf(target, ancestor) === target, target.value, Object.keys(target).join(','), Object.getPrototypeOf(target) === ancestor);
Object.setPrototypeOf(target, null);
console.log('null', Object.getPrototypeOf(target), target instanceof Object, target.value);
Object.setPrototypeOf(target, Object.prototype);
console.log('restore', target instanceof Object, target.constructor === Object);
console.log('primitives', Object.getPrototypeOf(1) === Number.prototype, Object.getPrototypeOf('a') === String.prototype, Object.getPrototypeOf(true) === Boolean.prototype);
console.log('primitive-set', Object.setPrototypeOf(1, null), Object.setPrototypeOf('a', {}), Object.setPrototypeOf(false, null));
for (const value of [null, undefined]) {
    try {Object.getPrototypeOf(value);} catch(error) {console.log('get-nullish', error instanceof TypeError);}
    try {Object.setPrototypeOf(value, {});} catch(error) {console.log('set-nullish', error instanceof TypeError);}
}
try {Object.setPrototypeOf(1, 2 as any);} catch(error) {console.log('invalid-prototype', error instanceof TypeError);}
const a: any = {}, b: any = {};
Object.setPrototypeOf(a, b);
try {Object.setPrototypeOf(b, a);} catch(error) {console.log('cycle', error instanceof TypeError);}
try {Object.setPrototypeOf(Object.prototype, {});} catch(error) {console.log('immutable', error instanceof TypeError);}
console.log('unchanged', Object.setPrototypeOf(Object.prototype, null) === Object.prototype);
let effects = 0;
const fake = {get __proto__(): unknown {effects++;return 9;}};
console.log('internal', Object.getPrototypeOf(fake) === Object.prototype, effects);

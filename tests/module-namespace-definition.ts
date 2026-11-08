import * as namespace from './module-namespace-order-values.js';
const key = 'changing';
console.log('compatible', Reflect.defineProperty(namespace, key, {}), Reflect.defineProperty(namespace, key, {value: 1}), Reflect.defineProperty(namespace, key, {writable: true, enumerable: true, configurable: false}));
console.log('rejected', Reflect.defineProperty(namespace, key, {value: 3}), Reflect.defineProperty(namespace, key, {writable: false}), Reflect.defineProperty(namespace, key, {get: undefined}), Reflect.defineProperty(namespace, 'extra', {value: 1}));
console.log('tag', Reflect.defineProperty(namespace, Symbol.toStringTag, {value: 'Module'}), Reflect.defineProperty(namespace, Symbol.toStringTag, {writable: true}), Reflect.defineProperty(namespace, Symbol.toStringTag, {value: 'other'}));
console.log('object', Object.defineProperty(namespace, key, {}) === namespace, Object.seal(namespace) === namespace, Object.isSealed(namespace), Object.isFrozen(namespace));
try {Object.freeze(namespace);} catch (error) {console.log('freeze', (error as Error).name);}
namespace.update();
console.log('live', namespace.changing, Reflect.defineProperty(namespace, key, {value: 1}), Reflect.defineProperty(namespace, key, {value: 2}));

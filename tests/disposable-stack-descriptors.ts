/// <reference lib="esnext.disposable" />
export {};
let effects = '';
const stack = new DisposableStack();
Object.defineProperty(stack, 'label', {value: 'stack', enumerable: true});
Object.freeze(stack);
stack.defer(() => {effects += 'A';});
stack.adopt(7, value => {effects += value;});
const moved = stack.move();
console.log('move', stack.disposed, moved.disposed, Object.isFrozen(stack), Object.isExtensible(moved), Object.hasOwn(moved, 'label'));
moved.dispose();
console.log('disposed', moved.disposed, effects, Reflect.set(stack, 'label', 'other'));
const direct = Object.freeze(new DisposableStack());
direct.defer(() => {effects += 'D';});
direct.dispose();
console.log('direct', direct.disposed, effects);
const asyncStack = new AsyncDisposableStack();
Object.freeze(asyncStack);
asyncStack.defer(async () => {effects += 'X';});
asyncStack.disposeAsync().then(() => console.log('async disposed', asyncStack.disposed, Object.isFrozen(asyncStack), effects));
for (const prototype of [DisposableStack.prototype, AsyncDisposableStack.prototype]) {
    const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
}

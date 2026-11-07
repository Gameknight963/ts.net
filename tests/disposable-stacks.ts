/// <reference lib="esnext.disposable" />
const stack = new DisposableStack();
console.log("new", stack.disposed, stack instanceof DisposableStack, Object.prototype.toString.call(stack));
const resource = {[Symbol.dispose]() {console.log("resource", this === resource);}};
console.log(stack.use(resource) === resource, stack.use(null), stack.use(undefined));
console.log("adopted", stack.adopt(12, value => console.log("adopt", value)));
stack.defer(function(this: unknown) {console.log("defer", this === undefined, stack.disposed);});
console.log("dispose result", stack.dispose(), stack.disposed);
stack.dispose();
for (const action of [() => stack.use(null), () => stack.defer(() => {}), () => stack.adopt(1, () => {}), () => stack.move()]) {
    try {action();} catch (error) {console.log("disposed error", (error as Error).name);}
}
const original = new DisposableStack(); original.defer(() => console.log("moved cleanup"));
const moved = original.move(); console.log("move", original.disposed, moved.disposed, moved instanceof DisposableStack);
original.dispose(); moved.dispose();
try {
    using errors = new DisposableStack();
    errors.defer(() => {throw "first";}); errors.defer(() => {throw "second";});
    throw "body";
} catch(error) {let value: any = error; while(value instanceof SuppressedError) {console.log("suppressed", value.error instanceof SuppressedError ? "nested:" + value.error.error + ":" + value.error.suppressed : value.error); value = value.suppressed;} console.log("original error", value);}
class Stack extends DisposableStack {name = "subclass";}
const derived = new Stack(); derived.defer(() => console.log(derived.name));
const transferred = derived.move(); console.log("subclass", derived instanceof Stack, derived instanceof DisposableStack, transferred instanceof Stack); transferred.dispose();
console.log("alias", DisposableStack.prototype.dispose === DisposableStack.prototype[Symbol.dispose]);
try {(DisposableStack as any)();} catch(e) {console.log("requires new", (e as Error).name);}
try {DisposableStack.prototype.use.call({} as any, null);} catch(e) {console.log("brand", (e as Error).name);}

const invalid = new DisposableStack();
for (const action of [() => invalid.use(1 as any), () => invalid.defer(1 as any), () => invalid.adopt(1, null as any)]) {
    try {action();} catch(error) {console.log("invalid", (error as Error).name, invalid.disposed);}
}
invalid.defer(() => {try {invalid.defer(() => {});} catch(error) {console.log("reentrant", (error as Error).name);}});
invalid.dispose();

/// <reference lib="esnext.disposable" />
async function main() {
    const stack = new AsyncDisposableStack();
    console.log("new", stack.disposed, stack instanceof AsyncDisposableStack, Object.prototype.toString.call(stack));
    stack.use({async [Symbol.asyncDispose]() {console.log("async start"); await Promise.resolve(); console.log("async end");}});
    stack.use({[Symbol.dispose]() {console.log("sync fallback");}});
    console.log("adopted", stack.adopt(7, async value => {await Promise.resolve(); console.log("adopt", value);}));
    stack.defer(async function(this: unknown) {console.log("defer", this === undefined, stack.disposed); await Promise.resolve(); console.log("defer end");});
    await stack.disposeAsync(); console.log("disposed", stack.disposed); await stack.disposeAsync();
    for (const action of [() => stack.use(null), () => stack.defer(() => {}), () => stack.adopt(1, () => {}), () => stack.move()]) {
        try {action();} catch (error) {console.log("disposed error", (error as Error).name);}
    }
    const original = new AsyncDisposableStack(); original.defer(() => console.log("moved cleanup"));
    const moved = original.move(); console.log("move", original.disposed, moved.disposed); await original.disposeAsync(); await moved.disposeAsync();
    try {
        await using errors = new AsyncDisposableStack();
        errors.defer(async () => {throw "first";}); errors.defer(async () => {throw "second";}); throw "body";
    } catch(error) {let value: any = error; while(value instanceof SuppressedError) {console.log("suppressed", value.error instanceof SuppressedError ? "nested:" + value.error.error + ":" + value.error.suppressed : value.error); value = value.suppressed;} console.log("original error", value);}
    class Stack extends AsyncDisposableStack {name = "subclass";}
    const derived = new Stack(); derived.defer(() => console.log(derived.name));
    const transferred = derived.move(); console.log("subclass", derived instanceof Stack, transferred instanceof Stack); await transferred.disposeAsync();
    console.log("alias", AsyncDisposableStack.prototype.disposeAsync === AsyncDisposableStack.prototype[Symbol.asyncDispose]);
    try {await AsyncDisposableStack.prototype.disposeAsync.call({} as any);} catch(e) {console.log("brand", (e as Error).name);}
    const invalid = new AsyncDisposableStack();
    for (const action of [() => invalid.use(1 as any), () => invalid.defer(1 as any), () => invalid.adopt(1, null as any)]) {
        try {action();} catch(error) {console.log("invalid", (error as Error).name, invalid.disposed);}
    }
    invalid.defer(() => {try {invalid.defer(() => {});} catch(error) {console.log("reentrant", (error as Error).name);}});
    await invalid.disposeAsync();

}
main();

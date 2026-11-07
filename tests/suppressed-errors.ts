/// <reference lib="esnext.disposable" />
const reason = {value: 1}, previous = {value: 2};
for (const error of [new SuppressedError(reason, previous, "message"), SuppressedError(reason, previous)]) {
    console.log("error", error.name, error.message, error.error === reason, error.suppressed === previous, error instanceof SuppressedError, error instanceof Error, String(error));
    for (const key of ["error", "suppressed"]) {
        const descriptor = Object.getOwnPropertyDescriptor(error, key)!;
        console.log("descriptor", descriptor.writable, descriptor.enumerable, descriptor.configurable);
    }
}
class Specialized extends SuppressedError {}
const specialized = new Specialized(reason, previous, "child");
console.log("subclass", specialized instanceof Specialized, specialized instanceof SuppressedError, specialized.error === reason);
let reads = 0;
const ignored = new (SuppressedError as any)(1, 2, "ignored", {get cause() {reads++; return 3;}});
console.log("ignored options", reads, Object.hasOwn(ignored, "cause"));
console.log("metadata", SuppressedError.name, SuppressedError.length, Object.getPrototypeOf(SuppressedError) === Error, SuppressedError.prototype.constructor === SuppressedError);

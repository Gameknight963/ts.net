export {};
function target(value: number): number { return value; }
function install(callback: Function): void {
    Object.defineProperty(target, "call", { configurable: true, value: callback });
}
install((receiver: { base: number }, value: number): number => receiver.base + value);
console.log("pair", target.call({ base: 7 }, 3));
function factory(offset: number): Function {
    return (receiver: { base: number }, value: number): number => offset + receiver.base + value;
}
install(factory(11));
console.log("closure", target.call({ base: 7 }, 3));
install((receiver: any, value: number): number => value);
function missing(): number { return undefined as any; }
console.log("missing", target.call(null, missing()));
console.log("negative zero", Object.is(target.call(null, -0), -0));
install(function(this: any, receiver: any, value: number): number {
    console.log("receiver", this === target, arguments.length);
    return receiver.base + value;
});
console.log("ordinary fallback", target.call({ base: 7 }, 3));
install((receiver: any, value = 99): number => receiver.base + value);
console.log("default fallback", target.call({ base: 7 }, missing()));
install((receiver: any, ...values: number[]): number => receiver.base + values[0]);
console.log("rest fallback", target.call({ base: 7 }, 3));
install((receiver: any, value: number): number => { throw new Error("pair failure"); });
try { target.call(null, 3); } catch (error) { console.log((error as Error).message); }
delete (target as any).call;
console.log("intrinsic", target.call(null, 3));

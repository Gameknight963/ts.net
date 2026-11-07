var nativeTopLevelBinding = 7;
const global = globalThis as any;
if (global.nativeTopLevelBinding !== undefined) throw new Error("Compiler locals leaked into the global object");
global.nativeTopLevelBinding = 12;
if (nativeTopLevelBinding !== 7) throw new Error("Global property changed a compiler local");
let failed = false;
try {global.Math = {};} catch(error) {failed = error instanceof TypeError;}
if (!failed || global.Math !== Math) throw new Error("Native binding was replaced");
if (Reflect.set(global,"Math",{}) !== false) throw new Error("Reflect.set accepted a native binding replacement");
if (Reflect.deleteProperty(global,"Math") !== false) throw new Error("Native binding was deleted");
const descriptor = Object.getOwnPropertyDescriptor(global,"Math")!;
if (descriptor.writable || descriptor.enumerable || descriptor.configurable) throw new Error("Native binding descriptor is mutable");
delete global.nativeTopLevelBinding;
console.log("PASS native global bindings stay protected and compiler locals remain independent");

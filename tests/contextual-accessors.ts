function write(target: {value: number}): void { target.value = 7; }
let stored = 0;
write({set value(value: number) { stored = value; }});
console.log("setter", stored);
function describe(target: object): void {
    const descriptor = Object.getOwnPropertyDescriptor(target, "value")!;
    console.log("descriptor", typeof descriptor.get, typeof descriptor.set, descriptor.enumerable);
}
describe({set value(value: number) {throw "setter"; }});
describe({get value(): number {throw "getter"; }});
try { write({set value(value: number) {throw "setter failure"; }}); } catch (error) {console.log("throw", error);}
const typed: {value: number} = {get value() {return stored;}, set value(value: number) {stored = value;}};
typed.value = 9; console.log("typed", typed.value, stored);

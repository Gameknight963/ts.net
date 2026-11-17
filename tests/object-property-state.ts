export {};
function make(accessorFirst: boolean): any {
 const value = Object.create(null);
 function accessor() {
  Object.defineProperty(value, "double", {configurable: true, enumerable: true,
   get: function(this: any) {return this.base * 2;},
   set: function(this: any, input: number) {this.base = input / 2;}});
 }
 if (accessorFirst) accessor();
 value.base = 7;
 if (!accessorFirst) accessor();
 return value;
}
for (const first of [false, true]) {
 const value = make(first);
 console.log("created", first, value.base, value.double, Object.keys(value).join(","));
 value.double = 20;
 console.log("setter", value.base, value.double);
 const copy = {...value};
 copy.base = 100;
 console.log("copy", copy.base, copy.double, value.base, value.double);
 Object.defineProperty(value, "double", {value: 31, configurable: true, writable: true, enumerable: true});
 console.log("data", value.base, value.double);
 delete value.base;
 value.base = 11;
 Object.defineProperty(value, "double", {configurable: true, enumerable: true, get: function(this: any) {return this.base + 1;}});
 console.log("accessor again", value.double, Object.keys(value).join(","));
 Object.seal(value);
 console.log("sealed", Object.isSealed(value), value.double, Object.getOwnPropertyDescriptor(value, "double")!.configurable);
 value.base = 13;
 console.log("sealed write", value.double);
 Object.freeze(value);
 console.log("frozen", Object.isFrozen(value), value.double, Object.getOwnPropertyDescriptor(value, "base")!.writable);
}
const accessorOnly = Object.create(null);
Object.defineProperty(accessorOnly, "value", {get() {return 17;}, configurable: true, enumerable: true});
console.log("only accessor", accessorOnly.value, Object.keys(accessorOnly).join(","));
Object.preventExtensions(accessorOnly);
console.log("nonextensible accessor", accessorOnly.value, Object.isExtensible(accessorOnly));
const dataOnly = Object.create(null);
dataOnly.value = 19;
Object.preventExtensions(dataOnly);
console.log("nonextensible data", dataOnly.value, Object.isExtensible(dataOnly));

const typed = {base: 23};
Object.defineProperty(typed, "extra", {value: 29, enumerable: true, configurable: true, writable: true});
const typedCopy = {...typed};
Object.defineProperty(typedCopy, "extra", {value: 31, enumerable: true, configurable: true, writable: true});
console.log("typed extra copy", typed.base, (typed as any).extra, typedCopy.base, (typedCopy as any).extra);
delete (typedCopy as any).extra;
console.log("typed extra deletion", (typed as any).extra, Object.hasOwn(typedCopy, "extra"));

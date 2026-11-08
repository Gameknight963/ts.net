export {};
function ordinary() {return arguments;}
const arrow = () => 1;
class Example {}
for (const value of [ordinary, arrow, Example, ordinary.bind(null), Math.abs, Function, Function.prototype]) {
    for (const key of ['caller', 'arguments']) {
        console.log('property', key, key in value, Object.hasOwn(value, key));
        try {console.log('read', (value as any)[key]);} catch (error) {console.log('read error', (error as Error).name);}
        try {(value as any)[key] = 1; console.log('write allowed');} catch (error) {console.log('write error', (error as Error).name);}
    }
}
const caller = Object.getOwnPropertyDescriptor(Function.prototype, 'caller')!;
const args = Object.getOwnPropertyDescriptor(Function.prototype, 'arguments')!;
console.log('descriptors', caller.enumerable, caller.configurable, args.enumerable, args.configurable, caller.get === caller.set, caller.get === args.get, args.get === args.set);
const callee = Object.getOwnPropertyDescriptor(ordinary(), 'callee')!;
console.log('shared thrower', callee.get === caller.get, callee.set === caller.set);
const thrower = caller.get!;
console.log('thrower', thrower.name, thrower.length, Object.isExtensible(thrower), Object.getOwnPropertyDescriptor(thrower, 'name')!.configurable, Object.getOwnPropertyDescriptor(thrower, 'length')!.configurable);
for (const value of [null, {}, ordinary]) try {thrower.call(value);} catch (error) {console.log('borrowed error', (error as Error).name);}
Object.defineProperty(ordinary, 'caller', {value: 7, writable: true, configurable: true});
console.log('shadowed', (ordinary as any).caller, Object.hasOwn(ordinary, 'caller'));
delete (ordinary as any).caller;
try {(ordinary as any).caller;} catch (error) {console.log('restored error', (error as Error).name);}

console.log('callee thrower', callee.get!.name, callee.get!.length, Object.isExtensible(callee.get), Object.getOwnPropertyDescriptor(callee.get!, 'name')!.configurable, Object.getOwnPropertyDescriptor(callee.get!, 'length')!.configurable, callee.get === callee.set);

export {};
const direct = {remove: 1, keep: 2};
for (const erase of [Reflect.deleteProperty]) console.log(erase(direct, 'remove'), Object.keys(direct).join(','));
const functions = [Reflect.deleteProperty];
const aliases = functions;
const assigned = {remove: 3, keep: 4};
let erase = Reflect.deleteProperty;
for (erase of aliases) console.log(erase(assigned, 'remove'), Object.keys(assigned).join(','));
const paired = {remove: 5, keep: 6};
for (const [name, remove] of [['remove', Reflect.deleteProperty] as const]) console.log(remove(paired, name), Object.keys(paired).join(','));
const definitions = {value: 1};
for (const {define} of [{define: Object.defineProperty}]) {
    define(definitions, 'value', {get() { return 7; }, configurable: true});
    console.log(definitions.value, Object.getOwnPropertyDescriptor(definitions, 'value')!.get !== undefined);
}
const namespaceTarget = {remove: 8, keep: 9};
for (const namespace of [Reflect]) console.log(namespace.deleteProperty(namespaceTarget, 'remove'), Object.keys(namespaceTarget).join(','));
function source(): ReadonlyArray<typeof Reflect.deleteProperty> { return [Reflect.deleteProperty]; }
const returned = {remove: 10, keep: 11};
for (const remove of source()) console.log(remove(returned, 'remove'), Object.keys(returned).join(','));
const repeated = {remove: 12, keep: 13};
let recursive = Reflect.deleteProperty;
for (recursive of [recursive]) console.log(recursive(repeated, 'remove'), Object.keys(repeated).join(','));

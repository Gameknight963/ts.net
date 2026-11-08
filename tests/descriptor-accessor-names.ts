export {};
let stored = 1;
function readValue(): number {return stored;}
function writeValue(value: number): void {stored = value;}
console.log('before', readValue.name, writeValue.name);
const value = Object.create(null, {
    first: {get: readValue, set: writeValue},
    second: {get: readValue, set: writeValue}
});
console.log('after', readValue.name, writeValue.name);
for (const key of ['first', 'second']) {
    const descriptor = Object.getOwnPropertyDescriptor(value, key)!;
    console.log('descriptor', descriptor.get === readValue, descriptor.set === writeValue, descriptor.get!.name, descriptor.set!.name);
}
value.first = 7;
console.log('value', value.second);
const descriptor = {get: function () {return 42;}, set: function (value: number) {stored = value;}};
console.log('inferred before', descriptor.get.name, descriptor.set.name);
const inferred = Object.create(null, {other: descriptor});
console.log('inferred after', descriptor.get.name, descriptor.set.name, inferred.other);

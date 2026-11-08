export {};
const buffer = new SharedArrayBuffer(8);
const alias = structuredClone(buffer);
let destination = buffer;
class Aliased extends Uint8Array {
    static get [Symbol.species](): any {
        return class {constructor(length: number) {return new Uint8Array(destination, 1, length);}};
    }
}
const source = new Aliased(buffer as any, 0, 6);
console.log('wrappers', buffer === alias);
for (const target of [buffer, alias]) {
    destination = target;
    source.set([1, 2, 3, 4, 5, 6]);
    console.log('slice', source.slice(0, 4).join(','), new Uint8Array(buffer).join(','));
}

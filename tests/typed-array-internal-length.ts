export {};
function make(): Uint8Array {
    const array = new Uint8Array([1, 2, 3]);
    Object.defineProperty(array, 'length', {get() {throw new Error('public length');}});
    return array;
}
const array = make();
console.log('read', array.join(','), array.at(1), array.includes(3), array.indexOf(2), array.lastIndexOf(2));
console.log('fill', array.fill(7, 1) === array, array.join(','));
const copied = make();
console.log('copy', copied.copyWithin(1, 0, 2) === copied, copied.join(','));
const reversed = make();
console.log('reverse', reversed.reverse() === reversed, reversed.join(','));
try {Array.prototype.join.call(array, ',');} catch (error) {console.log('generic', (error as Error).message);}
const hidden = new Uint8Array([4, 5]);
Object.setPrototypeOf(hidden, null);
console.log('hidden', Uint8Array.prototype.join.call(hidden, ','), Uint8Array.prototype.indexOf.call(hidden, 5), Uint8Array.prototype.at.call(hidden, 1));

export {};
let escaped: any;
function CapturingSpecies(length: number): any {escaped = new Array(length).fill(0); return escaped;}
const custom = [1, 2, 3];
Object.defineProperty(custom, 'constructor', {value: {[Symbol.species]: CapturingSpecies}});
try {
    custom.map((value, index) => {if (index === 0) Object.freeze(escaped); return value;});
} catch(error) {console.log('escaped map result', (error as Error).name, escaped[0], Object.isFrozen(escaped));}
let setters = 0;
const source = [1, 2, 3];
const mapped = source.map((value, index, array) => {
    if (index === 0) {
        Object.defineProperty(Array.prototype, '0', {set(value: unknown) {setters++;}, configurable: true});
        array.length = 1;
        Object.defineProperty(Array.prototype, '1', {get() {return 5;}, configurable: true});
    }
    return value * 2;
});
Reflect.deleteProperty(Array.prototype, '0');
Reflect.deleteProperty(Array.prototype, '1');
console.log('private map result', setters, mapped.length, Object.keys(mapped).join(','), JSON.stringify(mapped));
console.log('own mapped values', Object.hasOwn(mapped, '0'), Object.hasOwn(mapped, '1'), Object.hasOwn(mapped, '2'));

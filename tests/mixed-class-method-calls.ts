class FirstMethod { base = 2; read(value: number): number { return this.base + value; } }
class SecondMethod { base = 3; read(value: number): number { return this.base * 2 + value; } }
type Reader = {read(value: number): number};
function invokeMixed(target: Reader, value: number): number { return target.read(value); }
const first = new FirstMethod(), second = new SecondMethod();
let total = 0;
for (let index = 0; index < 64; index++) total += invokeMixed(index & 1 ? first : second, index);
console.log('mixed', total);
console.log('structural', invokeMixed({read(value) { return value + 10; }}, 4));
console.log('borrowed', invokeMixed({read: second.read, base: 9} as Reader, 4));
let fieldReads = 0, methodReads = 0;
Object.defineProperty(second, 'base', {configurable: true, get() { return ++fieldReads; }});
console.log('field-getter', invokeMixed(second, 4), invokeMixed(second, 4), fieldReads);
const original = SecondMethod.prototype.read;
const descriptor = Object.getOwnPropertyDescriptor(SecondMethod.prototype, 'read')!;
try {
    Object.defineProperty(SecondMethod.prototype, 'read', {configurable: true, get() { methodReads++; return original; }});
    console.log('method-getter', invokeMixed(second, 4), invokeMixed(second, 4), methodReads, fieldReads);
} finally { Object.defineProperty(SecondMethod.prototype, 'read', descriptor); }
Object.defineProperty(second, 'read', {value: (value: number) => value + 20, configurable: true});
console.log('replacement', invokeMixed(second, 4));
delete (second as any).read;
console.log('restored', invokeMixed(second, 4), fieldReads);
class DerivedMethod extends SecondMethod { read(value: number): number { return value + 30; } }
console.log('derived', invokeMixed(new DerivedMethod(), 4));
function replaceArgument(): number {
    Object.defineProperty(second, 'read', {value: (value: number) => value + 40, configurable: true});
    return 5;
}
console.log('captured-method', second.read(replaceArgument()), invokeMixed(second, 5), fieldReads);

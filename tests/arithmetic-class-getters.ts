export {};
class Values {
    a = 8;
    b = 2;
    get sum(): number {return this.a + this.b;}
    get difference(): number {return this.a - this.b;}
    get product(): number {return this.a * this.b;}
    get quotient(): number {return this.a / this.b;}
    get nested(): number {return -(this.a + 1) * (+this.b - 3) / 2;}
    get negative(): number {return -this.a;}
    get literal(): number {return (1 + 2) * 3 - 4 / 2;}
}
function show(value: Values) {console.log(value.sum, value.difference, value.product, value.quotient, value.nested, value.negative, value.literal);}
const value = new Values(); show(value);
value.a = 11; value.b = 4; show(value);
value.a = undefined as any; show(value);
value.a = -0; value.b = 0; show(value);
console.log('sign', Object.is(value.negative, 0), Object.is(value.product, -0));
value.a = Infinity; value.b = 2; show(value);
value.a = 3;
const sum = Object.getOwnPropertyDescriptor(Values.prototype, 'sum')!.get!;
Object.defineProperty(Values.prototype, 'sum', {get() {return 99;}, configurable: true}); show(value);
Object.defineProperty(Values.prototype, 'sum', {get: sum, configurable: true}); show(value);
Object.defineProperty(value, 'sum', {value: 71, configurable: true}); show(value);
delete (value as any).sum; show(value);
let calls = 0;
Object.defineProperty(value, 'a', {get() {calls++; return calls;}, configurable: true});
Object.defineProperty(value, 'b', {get() {calls++; return calls;}, configurable: true});
console.log('left first', value.sum, calls);
console.log('nested order', value.nested, calls);
Object.defineProperty(value, 'a', {get() {throw new Error('field read');}, configurable: true});
try {console.log(value.sum);} catch (error) {console.log((error as Error).message);}
class Child extends Values {get sum(): number {return 101;}}
console.log('derived', new Child().sum);

let offset = 3;
class Captured {
    base = 2;
    get value(): number {return this.base + offset;}
    get count(): number {return this.base + arguments.length;}
    get large(): number {return this.base + (1 + (2 + (3 + (4 + (5 + (6 + (7 + (8 + 9))))))));}
}
const captured = new Captured(); console.log('fallbacks', captured.value, captured.count, captured.large);
offset = 7; console.log('changed external', captured.value);
Object.defineProperty(value, 'a', {get() {
    Object.defineProperty(Values.prototype, 'sum', {get() {return 500;}, configurable: true});
    return 2;
}, configurable: true});
console.log('replacement during read', value.sum, value.sum);

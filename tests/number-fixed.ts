const values = [0, -0, 0.1, -0.1, 1.25, -1.25, 2.55, 2.45, 1.005, 9.999, 99.95, 1000000000000000128, 1e20, 1e21, Number.MIN_VALUE, Number.MAX_VALUE, NaN, Infinity, -Infinity];
for (let index = 0; index < values.length; index++) for (const digits of [0, 1, 2, 5, 20, 100]) console.log('fixed', index, digits, values[index].toFixed(digits));
console.log('defaults', (1.25).toFixed(), (1.25).toFixed(undefined));
for (const digits of [-1, 101, Infinity, -Infinity]) {
    for (const value of [1, NaN, Infinity]) {
        try {value.toFixed(digits);} catch(error) {console.log('invalid', (error as Error).name);}
    }
}
const fixed = Number.prototype.toFixed as any;
console.log('coercion', fixed.call(1.25, '1'), fixed.call(1.25, -0.9), fixed.call(1.25, NaN));
const digits = {valueOf(): number {console.log('digits');return 1;}};
console.log('object digits', fixed.call(1.25, digits));
try {fixed.call('1.25', digits);} catch(error) {console.log('receiver first', (error as Error).name);}
try {fixed.call(null);} catch(error) {console.log('null', (error as Error).name);}
const boxed = new Number(1.25);
console.log('boxed', boxed.toFixed(1), boxed.valueOf(), boxed instanceof Number, typeof boxed);
console.log('prototype', Number.prototype.valueOf(), Number.prototype.toString(), Number.prototype.toFixed(2));
class Quantity extends Number {label = 'quantity';}
const quantity = new Quantity(2.55);
console.log('derived', quantity.toFixed(1), quantity.valueOf(), quantity.label, quantity instanceof Quantity, quantity instanceof Number);
console.log('metadata', fixed.name, fixed.length, Object.keys(Number.prototype).length);

for (let index = -64; index <= 64; index++) for (let digits = 0; digits <= 5; digits++) console.log('ties', index, digits, (index / 8).toFixed(digits));
console.log('construction', new Number().valueOf(), new Number(undefined).valueOf(), new Number('12.5').valueOf(), Number(new Number(7)), Object.keys(new Number(3)).length);

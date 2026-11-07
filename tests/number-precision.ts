const values = [0, -0, 1, -1, 1.25, -1.25, 9.999, 99.95, 0.000001, 0.0000009999999999, 1e-7, 1e20, 1e21, 1e23, 1000000000000000128, Number.MIN_VALUE, Number.MAX_VALUE, NaN, Infinity, -Infinity];
for (let index = 0; index < values.length; index++) {
    const value = values[index];
    console.log('default', index, value.toExponential(), value.toPrecision());
    for (const digits of [0, 1, 2, 5, 17, 100, 101, -1, NaN, Infinity]) {
        for (const method of [Number.prototype.toExponential, Number.prototype.toPrecision]) {
            try {console.log('formatted', index, digits, method.name, method.call(value, digits));} catch(error) {console.log('error', index, digits, method.name, (error as Error).name);}
        }
    }
}
for (let index = -64; index <= 64; index++) for (let digits = 1; digits <= 6; digits++) console.log('ties', index, digits, (index / 8).toExponential(digits - 1), (index / 8).toPrecision(digits));
const count = {valueOf(): number {console.log('digits');return 2;}};
for (const method of [Number.prototype.toExponential, Number.prototype.toPrecision]) {
    console.log('coercion', (method as any).call(1.25, count));
    console.log('boxed', method.call(new Number(1.25), 2), method.call(Number.prototype, 2));
    try {(method as any).call('1.25', count);} catch(error) {console.log('receiver first', (error as Error).name);}
    try {(method as any).call(NaN, {valueOf(): never {throw 'digit failure';}});} catch(error) {console.log('conversion first', error);}
    console.log('metadata', method.name, method.length, Object.keys(Number.prototype).includes(method.name));
}

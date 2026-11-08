export {};
function source(): any {
    const result: any = Object.create(null, {
        a: {enumerable: true, get() {delete result.b; result.b = 2; return 1;}},
        b: {value: 0, writable: true, enumerable: false, configurable: true}
    });
    return result;
}
console.log('assign', JSON.stringify(Object.assign({}, source())));
console.log('spread', JSON.stringify({...source()}));
const symbol = Symbol('later');
function symbolSource(): any {
    const result: any = Object.create(null, {
        a: {enumerable: true, get() {delete result[symbol]; result[symbol] = 3; return 1;}},
        [symbol]: {value: 0, writable: true, enumerable: false, configurable: true}
    });
    return result;
}
const assigned = Object.assign({}, symbolSource()), spread = {...symbolSource()};
console.log('symbols', assigned[symbol], spread[symbol], Object.getOwnPropertySymbols(assigned).length, Object.getOwnPropertySymbols(spread).length);

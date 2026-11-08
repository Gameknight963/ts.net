export {};
const symbol = Symbol('later');
function source(): any {
    const result: any = Object.create(null, {
        skip: {value: 42, enumerable: true},
        a: {enumerable: true, get() {
            delete result.b; result.b = 2;
            delete result[symbol]; result[symbol] = 3;
            return 1;
        }},
        b: {value: 0, writable: true, enumerable: false, configurable: true},
        [symbol]: {value: 0, writable: true, enumerable: false, configurable: true},
        hidden: {get() {throw new Error('hidden');}}
    });
    return result;
}
const {skip, ...rest} = source();
console.log('declaration', skip, JSON.stringify(rest), rest[symbol]);
let assigned: any;
({skip: assigned, ...assigned} = source());
console.log('assignment', JSON.stringify(assigned), assigned[symbol]);
const {a, ...excluded} = source();
console.log('excluded', a, JSON.stringify(excluded), excluded[symbol]);
async function run(): Promise<void> {
    const {missing = await Promise.resolve(7), skip, ...rest} = source();
    console.log('await', missing, skip, JSON.stringify(rest), rest[symbol]);
}
run();

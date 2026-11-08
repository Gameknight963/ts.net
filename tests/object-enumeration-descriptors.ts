export {};
function source(): any {
    const result: any = Object.create(null, {
        a: {enumerable: true, get() {delete result.b; result.b = 2; return 1;}},
        b: {value: 0, writable: true, enumerable: false, configurable: true},
        hidden: {get() {throw new Error('must not read hidden');}}
    });
    return result;
}
console.log('keys', JSON.stringify(Object.keys(source())));
console.log('values', JSON.stringify(Object.values(source())));
console.log('entries', JSON.stringify(Object.entries(source())));
const typed: Record<string, number> = source();
console.log('numeric values', Object.values(typed).join(','));
function deleted(): any {
    const result: any = Object.create(null, {
        a: {enumerable: true, get() {delete result.b; result.c = 3; return 1;}},
        b: {value: 2, enumerable: true, configurable: true}
    });
    return result;
}
console.log('deleted values', JSON.stringify(Object.values(deleted())));
console.log('deleted entries', JSON.stringify(Object.entries(deleted())));

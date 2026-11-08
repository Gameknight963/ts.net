export {};
let setters = 0;
class Result {
    constructor() {
        return Object.create(null, {
            0: {value: 'old', writable: false, enumerable: false, configurable: true},
            1: {get() {return 'getter';}, set() {setters++;}, enumerable: false, configurable: true}
        });
    }
}
function show(label: string, result: any): void {
    console.log(label, result[0], result[1], result.length, setters);
    for (const key of ['0', '1']) {
        const descriptor = Object.getOwnPropertyDescriptor(result, key)!;
        console.log('descriptor', descriptor.writable, descriptor.enumerable, descriptor.configurable, descriptor.get, descriptor.set);
    }
}
show('sync', (Array.from as any).call(Result, [7, 8]));
class Fixed {
    constructor() {return Object.create(null, {0: {value: 1, writable: true, enumerable: true, configurable: false}});}
}
function source() {
    return {
        [Symbol.iterator]() {return this;},
        next() {return {done: false, value: 2};},
        return() {console.log('closed'); return {};}
    };
}
try {(Array.from as any).call(Fixed, source());}
catch (error) {console.log('fixed sync', (error as Error).name);}
(Array as any).fromAsync.call(Result, [9, 10]).then((result: any) => {
    show('async', result);
    return (Array as any).fromAsync.call(Fixed, source()).catch((error: Error) => console.log('fixed async', error.name));
});

function ClosedResult(): any {
    return Object.preventExtensions(Object.create(null, {
        '0': {get() {return 1;}, configurable: true},
        '1': {value: 2, writable: false, configurable: true},
        length: {value: 2, writable: true}
    }));
}
for (const factory of [Array.from, Array.of]) {
    const result = factory === Array.from ? (factory as any).call(ClosedResult, [11, 12]) : (factory as any).call(ClosedResult, 13, 14);
    console.log('closed result', result[0], result[1], Object.isExtensible(result), Object.getOwnPropertyDescriptor(result, '0')!.writable, Object.getOwnPropertyDescriptor(result, '1')!.enumerable);
}

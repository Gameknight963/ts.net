export {};
for (const value of [Object.fromEntries([['a', 1], ['b', 2]]), JSON.parse('{"a":1,"b":2}'), new Object()]) {
    Object.defineProperty(value, 'a', {value: 7, writable: true, enumerable: true, configurable: true});
    Object.defineProperty(value, 'a', {enumerable: false});
    console.log('hidden', (value as any).a, Object.keys(value).join(','));
    let stored = 8;
    Object.defineProperty(value, 'a', {get() {return stored;}, set(next: number) {stored = next;}});
    (value as any).a = 9;
    console.log('accessor', (value as any).a, Object.getOwnPropertyDescriptor(value, 'a')!.enumerable);
    Object.defineProperty(value, 'a', {value: 10, writable: false});
    try {(value as any).a = 11;} catch (error) {console.log('readonly', (error as Error).name);}
    console.log('data', (value as any).a, Object.getOwnPropertyNames(value).join(','));
}

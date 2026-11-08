export {};
for (const freeze of [false, true]) for (const value of [new Uint8Array([1, 2]), new Float64Array([1, 2]), new BigInt64Array([1n, 2n]), new Uint8Array(0)] as any[]) {
    value.extra = 3;
    Object.defineProperty(value, 'hidden', {value: 4, writable: true, configurable: true});
    try {console.log('identity', freeze, (freeze ? Object.freeze : Object.seal)(value) === value);}
    catch (error) {console.log('error', freeze, (error as Error).name);}
    console.log('state', value.length, Object.isExtensible(value), Object.isSealed(value), Object.isFrozen(value));
    const extra = Object.getOwnPropertyDescriptor(value, 'extra')!;
    console.log('extra', extra.writable, extra.configurable);
    const hidden = Object.getOwnPropertyDescriptor(value, 'hidden')!;
    console.log('hidden', hidden.writable, hidden.configurable, hidden.enumerable);
    if (!freeze) {
        value[0] = typeof value[0] === 'bigint' ? 9n : 9;
        console.log('write', String(value[0]), Reflect.deleteProperty(value, '0'), Reflect.deleteProperty(value, 'extra'));
    }
}
for (const freeze of [false, true]) {
    const value = new Uint8Array([1]);
    Object.preventExtensions(value);
    for (let attempt = 0; attempt < 2; attempt++) {
        try {(freeze ? Object.freeze : Object.seal)(value); console.log('repeat ok', freeze, attempt);}
        catch (error) {console.log('repeat error', freeze, attempt, (error as Error).name, Object.isSealed(value));}
    }
}
for (const freeze of [false, true]) {
    const value = new Uint8Array([1]);
    for (let attempt = 0; attempt < 2; attempt++) {
        try {(freeze ? Object.freeze : Object.seal)(value); console.log('plain ok', freeze, attempt);}
        catch (error) {console.log('plain error', freeze, attempt, (error as Error).name, Object.isSealed(value));}
    }
}

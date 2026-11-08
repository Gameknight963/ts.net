/// <reference lib="esnext.iterator" />
export {};
const prototype: any = String.prototype;
const original = prototype[Symbol.iterator];
try {
    for (const disabled of [null, undefined]) {
        prototype[Symbol.iterator] = disabled;
        for (const value of ['abc', Object('abc')]) {
            try {
                const iterator = Iterator.from(value as any);
                console.log('created', typeof value);
                iterator.next();
            } catch (error) {console.log('error', typeof value, (error as Error).name);}
        }
    }
    prototype[Symbol.iterator] = 42;
    for (const value of ['abc', Object('abc')]) {
        try {Iterator.from(value as any);}
        catch (error) {console.log('noncallable', typeof value, (error as Error).name);}
    }
    let receiver: unknown, gets = 0;
    const source = {
        get next() {
            gets++;
            return function (this: unknown) {console.log('next receiver', this === source); return {done: true, value: 9};};
        }
    };
    prototype[Symbol.iterator] = function (this: unknown) {receiver = this; return source;};
    for (const value of ['abc', Object('abc')]) {
        gets = 0;
        const iterator = Iterator.from(value as any);
        console.log('custom', receiver === value, gets, JSON.stringify(iterator.next()), gets);
    }
    prototype[Symbol.iterator] = function () {return 1;};
    try {Iterator.from('abc');}
    catch (error) {console.log('primitive result', (error as Error).name);}
} finally {prototype[Symbol.iterator] = original;}

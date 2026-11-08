/// <reference lib="esnext.iterator" />
export {};
const prototype: any = Iterator.prototype;
const original = prototype[Symbol.iterator];
try {
    delete prototype[Symbol.iterator];
    for (const value of [[1, 2].values(), 'abc'[Symbol.iterator](), new Map([[1, 2]]).values(), Iterator.from([3]).map(x => x)]) {
        console.log('sync', JSON.stringify(Array.from(value as any)));
    }
    for (const value of [[1, 2].values(), 'abc'[Symbol.iterator](), new Map([[1, 2]]).values(), Iterator.from([3]).map(x => x)]) {
        (Array as any).fromAsync(value).then((result: unknown[]) => console.log('async', JSON.stringify(result)));
    }
} finally {prototype[Symbol.iterator] = original;}

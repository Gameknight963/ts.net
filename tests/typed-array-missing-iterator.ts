/// <reference lib="esnext.iterator" />
export {};
const prototype: any = Iterator.prototype;
const original = prototype[Symbol.iterator];
const sources: any[] = [[1, 2].values(), new Map([[1, 2]]).values(), Iterator.from([3]).map(x => x)];
try {
    delete prototype[Symbol.iterator];
    for (const source of sources) {
        console.log('empty', new Uint8Array(source).length, Uint8Array.from(source).length);
        source.length = 2;
        source[0] = 7;
        source[1] = 8;
        console.log('indexed', new Uint8Array(source).join(','), Uint8Array.from(source, value => Number(value) + 1).join(','));
        console.log('not consumed', JSON.stringify(source.next()));
    }
} finally {prototype[Symbol.iterator] = original;}

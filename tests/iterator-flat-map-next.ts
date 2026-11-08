/// <reference lib="esnext.iterator" />
export {};
for (const native of [false, true]) {
    let gets = 0, index = 0;
    const inner: any = {
        get next() {
            gets++;
            console.log('get next', gets);
            return function (this: any) {
                console.log('next receiver', this === inner);
                return {done: index >= 2, value: index++};
            };
        },
        return() { console.log('return receiver', this === inner); return {}; }
    };
    if (native) Object.setPrototypeOf(inner, Iterator.prototype);
    const iterable = {[Symbol.iterator]() {console.log('iterate'); return inner;}};
    for (const source of [inner, iterable]) {
        gets = 0; index = 0;
        console.log('case', native, source === inner);
        const helper: any = Iterator.from([1]).flatMap(() => source);
        console.log('lazy', gets);
        console.log('first', helper.next().value, gets);
        console.log('closed', helper.return().done, gets);
    }
}

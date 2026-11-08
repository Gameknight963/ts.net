export {};
const from = (ReadableStream as any).from as (value: unknown) => ReadableStream<any>;
async function main(): Promise<void> {
    for (const value of [undefined,null,3,{}, {[Symbol.iterator]: 1}, {[Symbol.asyncIterator]: 1}, {[Symbol.iterator]() {return 3;}}]) {
        try {from(value);} catch (error) {console.log('invalid source', (error as Error).name);}
    }
    for (const step of [3,null]) {
        const stream = from({[Symbol.iterator]() {return {next() {return step;}};}});
        try {await stream.getReader().read();} catch (error) {console.log('invalid step', (error as Error).name);}
    }
    const thrown = from({[Symbol.iterator]() {return {next() {throw 'next';}};}});
    try {await thrown.getReader().read();} catch (error) {console.log('next throw', error);}
    const rejected = from({[Symbol.asyncIterator]() {return {next() {return Promise.reject('async next');}};}});
    try {await rejected.getReader().read();} catch (error) {console.log('next reject', error);}

    let closeCalls = 0;
    const failedValue = from({[Symbol.iterator]() {return {
        next() {return {done: false,value: Promise.reject('value')};},
        return() {closeCalls++;throw 'close';}
    };}});
    try {await failedValue.getReader().read();} catch (error) {console.log('value reject', error, closeCalls);}

    for (const result of [null,3]) {
        const invalidReturn = from({[Symbol.asyncIterator]() {return {
            next() {return Promise.resolve({done: false,value: 1});},
            return() {return Promise.resolve(result);}
        };}});
        try {await invalidReturn.cancel('stop');} catch (error) {console.log('invalid return', (error as Error).name);}
    }
    const rejectedReturn = from({[Symbol.iterator]() {return {
        next() {return {done: false,value: 1};},
        return() {return {done: true,value: Promise.reject('return value')};}
    };}});
    const returnReader = rejectedReturn.getReader();await returnReader.read();
    try {await returnReader.cancel();} catch (error) {console.log('return value', error);}

    const noReturn = from([1,2]);console.log('missing return', await noReturn.cancel('stop'));
    const finished = from([]);const reader = finished.getReader();await reader.read();
    console.log('finished cancellation', await reader.cancel());
}
main().catch(error => console.log('failure', String(error)));

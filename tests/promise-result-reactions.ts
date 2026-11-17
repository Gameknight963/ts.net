export {};
const log: string[] = [];
let finish!: (value: number) => void;
const source = new Promise<number>(resolve => { finish = resolve; });
const child = source.then(value => { log.push('child:' + value); return value + 1; });
const first = child.then(value => { log.push('first:' + value); return value + 1; });
const second = child.then(value => { log.push('second:' + value); return value + 2; });
queueMicrotask(() => log.push('microtask'));
finish(3);
log.push('sync');
const reason = new Error('original');
const rejected = Promise.reject(reason).then(value => { log.push('unexpected'); return value; });
const propagated = rejected.catch(error => { log.push('rejected:' + (error === reason)); return 7; });
const identity = Promise.resolve(11).then().then(value => { log.push('identity:' + value); return value; });
const ignored = Promise.resolve(13).then(0 as any, {} as any).then(value => { log.push('ignored:' + value); return value; });
const thrown = Promise.resolve(0).then(() => { throw new Error('handler'); }).catch(error => {
    log.push('thrown:' + error.message); return 17;
});
const thenable = Promise.resolve(0).then(() => ({ then(resolve: (value: number) => void) {
    log.push('thenable'); resolve(19); resolve(23); throw new Error('after resolution');
} } as any)).then(value => { log.push('adopted:' + value); return value; });
const adopted = Promise.resolve(0).then(() => Promise.resolve(29)).then(value => { log.push('promise:' + value); return value; });
let self!: Promise<any>;
self = Promise.resolve(0).then(() => self);
const cycle = self.catch(error => { log.push('cycle:' + error.name); return 31; });
const callable = () => 37;
const functionResult = Promise.resolve(0).then(() => callable).then(value => {
    log.push('function:' + (value === callable)); return value();
});
let reentrant!: Promise<number>;
reentrant = Promise.resolve(0).then(() => {
    reentrant.then(value => log.push('inside:' + value));
    return 41;
});
Promise.all([first, second, propagated, identity, ignored, thrown, thenable, adopted, cycle, functionResult, reentrant]).then(values => {
    console.log('values', values.join(','));
    console.log('order', log.join(','));
});

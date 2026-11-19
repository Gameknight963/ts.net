async function sample(value: number): Promise<number> {
    return await Promise.resolve(value);
}
async function continued(value: number): Promise<number> {
    const result = await Promise.resolve(value);
    return result + 1;
}
async function maybe(value: number | undefined): Promise<number | undefined> {
    return await Promise.resolve(value);
}
async function main(): Promise<void> {
    const prototype = Promise.prototype as any;
    const constructor = Object.getOwnPropertyDescriptor(prototype, 'constructor')!;
    const resolve = Object.getOwnPropertyDescriptor(Promise, 'resolve')!;
    const then = Object.getOwnPropertyDescriptor(prototype, 'then')!;
    for (const value of [1, 0, -0, NaN, Infinity]) {
        console.log('value', String(await sample(value)), Object.is(await sample(value), value));
    }
    console.log('continuation', await continued(2));
    console.log('nullable', await maybe(undefined), await maybe(0));
    const jobs: string[] = [];
    const pending = continued(3).then(value => jobs.push('value:' + value));
    queueMicrotask(() => jobs.push('queued'));
    await pending;
    console.log('jobs', jobs.join(','));

    const captured: Promise<number>[] = [];
    Object.defineProperty(prototype, 'constructor', {
        configurable: true,
        get(this: Promise<number>) {captured.push(this); return Promise;}
    });
    let observed: Promise<number>;
    try {
        observed = sample(4);
        console.log('constructor getter', captured.length, captured[0] instanceof Promise);
    }
    finally {Object.defineProperty(prototype, 'constructor', constructor);}
    console.log('exposed promise', await captured[0], await observed!);

    const parent = Object.getPrototypeOf(prototype);
    const parentConstructor = Object.getOwnPropertyDescriptor(parent, 'constructor')!;
    let inheritedReads = 0;
    delete prototype.constructor;
    Object.defineProperty(parent, 'constructor', {get() {inheritedReads++; return Promise;}, configurable: true});
    let inherited: Promise<number>;
    try {inherited = sample(13);}
    finally {
        Object.defineProperty(parent, 'constructor', parentConstructor);
        Object.defineProperty(prototype, 'constructor', constructor);
    }
    console.log('inherited constructor', inheritedReads, await inherited!);

    const changes: string[] = [];
    Object.defineProperty(Promise, 'resolve', {
        configurable: true,
        get() {changes.push('resolve getter'); return resolve.value;}
    });
    function argument(): number {
        changes.push('argument');
        Object.defineProperty(Promise, 'resolve', {value() {throw new Error('replacement must not run');}, configurable: true});
        Object.defineProperty(prototype, 'constructor', {
            get() {changes.push('constructor getter'); return Promise;}, configurable: true
        });
        return 5;
    }
    let changed: Promise<number>;
    async function changing(): Promise<number> {return await Promise.resolve(argument());}
    try {changed = changing();}
    finally {
        Object.defineProperty(Promise, 'resolve', resolve);
        Object.defineProperty(prototype, 'constructor', constructor);
    }
    console.log('changed during argument', await changed!, changes.join(','));

    Object.defineProperty(Promise, 'resolve', {
        value(this: unknown, value: number) {
            changes.push('receiver:' + (this === Promise));
            return {then(accept: (value: number) => void) {changes.push('thenable'); queueMicrotask(() => accept(value + 10));}};
        }, configurable: true
    });
    try {console.log('custom resolve', await sample(6), await continued(7));}
    finally {Object.defineProperty(Promise, 'resolve', resolve);}
    console.log('custom trace', changes.slice(3).join(','));

    Object.defineProperty(prototype, 'then', {get() {throw new Error('await must not read then');}, configurable: true});
    try {console.log('internal then', await sample(8), await continued(9));}
    finally {Object.defineProperty(prototype, 'then', then);}

    const order: string[] = [];
    Object.defineProperty(prototype, 'constructor', {value: function Other() {}, configurable: true});
    try {
        const operation = continued(10);
        queueMicrotask(() => order.push('queued'));
        console.log('foreign constructor', await operation);
        order.push('resumed');
    }
    finally {Object.defineProperty(prototype, 'constructor', constructor);}
    console.log('foreign order', order.join(','));

    Object.defineProperty(Promise, 'resolve', {value() {throw new Error('resolve failure');}, configurable: true});
    try {await sample(11);} catch (error) {console.log('resolve rejected', (error as Error).message);}
    finally {Object.defineProperty(Promise, 'resolve', resolve);}
    Object.defineProperty(prototype, 'constructor', {get() {throw new Error('constructor failure');}, configurable: true});
    let rejected: Promise<number>;
    try {rejected = sample(12);}
    finally {Object.defineProperty(prototype, 'constructor', constructor);}
    try {await rejected!;} catch (error) {console.log('constructor rejected', (error as Error).message);}
}
main();

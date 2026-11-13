function resolveMaybe(value?: number): Promise<number | undefined> { return Promise.resolve(value); }
Promise.resolve(7).then(value => console.log('number', value));
resolveMaybe().then(value => console.log('undefined', value));
Promise.resolve(-0).then(value => console.log('zero', Object.is(value, -0)));
Promise.resolve(NaN).then(value => console.log('nan', Number.isNaN(value)));
Promise.resolve(Infinity).then(value => console.log('infinity', value));
Promise.reject(8).catch(error => console.log('reject', error));
const resolveDescriptor = Object.getOwnPropertyDescriptor(Promise, 'resolve')!;
const originalResolve = Promise.resolve;
let resolveReads = 0, resolveCalls = 0;
function replaceResolveArgument(): number {
    Object.defineProperty(Promise, 'resolve', {configurable: true, value: function(value: number) {
        resolveCalls++; return originalResolve.call(Promise, value + 20);
    }});
    return 5;
}
try {
    Object.defineProperty(Promise, 'resolve', {configurable: true, get() {
        resolveReads++; console.log('get', resolveReads);
        return function(this: unknown, value: number) {
            resolveCalls++; console.log('custom', this === Promise, arguments.length, value);
            return originalResolve.call(Promise, value + 10);
        };
    }});
    const custom = Promise.resolve(4);
    Object.defineProperty(Promise, 'resolve', resolveDescriptor);
    custom.then(value => console.log('custom-result', value));
    const captured = Promise.resolve(replaceResolveArgument());
    const replaced = Promise.resolve(6);
    Object.defineProperty(Promise, 'resolve', resolveDescriptor);
    captured.then(value => console.log('captured', value));
    replaced.then(value => console.log('replaced', value));
    console.log('calls', resolveCalls);
} finally { Object.defineProperty(Promise, 'resolve', resolveDescriptor); }
const rejectDescriptor = Object.getOwnPropertyDescriptor(Promise, 'reject')!;
try {
    Object.defineProperty(Promise, 'reject', {configurable: true, value: function(this: unknown, value: number) {
        console.log('custom-reject', this === Promise, arguments.length, value);
        return originalResolve.call(Promise, value + 30);
    }});
    const custom = Promise.reject(9);
    Object.defineProperty(Promise, 'reject', rejectDescriptor);
    custom.then(value => console.log('reject-result', value));
} finally { Object.defineProperty(Promise, 'reject', rejectDescriptor); }

const originalReject = Promise.reject;
try {
    Object.defineProperty(Promise, 'resolve', {value: originalReject, configurable: true});
    const rejected = Promise.resolve(11);
    Object.defineProperty(Promise, 'resolve', resolveDescriptor);
    rejected.catch(value => console.log('cross-reject', value));
    Object.defineProperty(Promise, 'reject', {value: originalResolve, configurable: true});
    const resolved = Promise.reject(12);
    Object.defineProperty(Promise, 'reject', rejectDescriptor);
    resolved.then(value => console.log('cross-resolve', value));
} finally {
    Object.defineProperty(Promise, 'resolve', resolveDescriptor);
    Object.defineProperty(Promise, 'reject', rejectDescriptor);
}

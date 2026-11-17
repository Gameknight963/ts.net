export {};
function native(): Promise<number> { return Promise.resolve(3).then(value => value + 1); }
const first = native(), second = native();
const original = Promise.prototype.then;
const saved: unknown[] = [];
(Promise.prototype as any).then = function(this: Promise<any>, fulfilled: any, rejected: any): any {
    saved.push(fulfilled);
    return original.call(this, fulfilled, rejected);
};
const overriddenFirst = native(), overriddenSecond = native();
Promise.prototype.then = original;
console.log('override identities', saved.length, saved[0] !== saved[1]);
const observed: unknown[] = [];
const custom = { then(callback: (value: number) => number): (value: number) => number { observed.push(callback); return callback; } };
function customCall(): (value: number) => number { return custom.then(value => value + 7); }
const customFirst = customCall(), customSecond = customCall();
console.log('custom identities', observed.length, customFirst !== customSecond, customFirst(3), customSecond(4));
const proxied: unknown[] = [];
const proxy = new Proxy(original, { apply(target, receiver, argumentsList) {
    proxied.push(argumentsList[0]); return Reflect.apply(target, receiver, argumentsList);
} });
(Promise.prototype as any).then = proxy;
const proxyFirst = native(), proxySecond = native();
Promise.prototype.then = original;
console.log('proxy identities', proxied.length, proxied[0] !== proxied[1]);
let gets = 0;
const getterPromise = Promise.resolve(11);
Object.defineProperty(getterPromise, 'then', { get() { gets++; return original; } });
function getterCall(): Promise<number> { return getterPromise.then(value => value + 1); }
const getterFirst = getterCall(), getterSecond = getterCall();
console.log('getter calls', gets);
function captured(offset: number): Promise<number> { return Promise.resolve(13).then(value => value + offset); }
const capturedFirst = captured(2), capturedSecond = captured(3);
function activation(): Promise<() => number> {
    return Promise.resolve(1).then(value => { let local = value; return () => ++local; });
}
Promise.all([activation(), activation()]).then(callbacks => {
    console.log('activations', callbacks[0] !== callbacks[1], callbacks[0](), callbacks[0](), callbacks[1]());
});
let defaults = 0;
function defaulted(): Promise<number> { return Promise.resolve(undefined).then((value: number = ++defaults) => value); }
Promise.all([defaulted(), defaulted()]).then(values => console.log('defaults', values.join(','), defaults));
Promise.all([first, second, overriddenFirst, overriddenSecond, proxyFirst, proxySecond, getterFirst, getterSecond, capturedFirst, capturedSecond]).then(values => {
    console.log('values', values.join(','));
});

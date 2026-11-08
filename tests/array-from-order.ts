let order = '';
const input = {
    get [Symbol.iterator]() {
        order += 'get;';
        return function(this: unknown) {
            order += 'iterator;';
            console.log('receiver', this === input);
            return { get next() { order += 'next;'; return function() { order += 'step;'; return {done:true,value:0}; }; } };
        };
    }
};
class Result { length = 0; constructor(...args: any[]) { order += 'constructor;'; console.log('arity', args.length); } }
const result = (Array.from as any).call(Result, input);
console.log('order', order, result instanceof Result, result.length);
order = '';
class Failure { constructor() { order += 'constructor;'; throw 'failed'; } }
try { (Array.from as any).call(Failure, input); }
catch (error) { console.log('failure', error, order); }
order = '';
const changing = {
    [Symbol.iterator]() { order += 'original;'; return [1,2].values(); }
};
class Mutating { length = 0; constructor() { changing[Symbol.iterator] = () => { throw 'replaced'; }; } }
const captured = (Array.from as any).call(Mutating, changing);
console.log('captured', order, captured[0], captured[1], captured.length);

const overridden = [1,2,3];
overridden[Symbol.iterator] = function*(): Generator<number, undefined, unknown> { yield 9; return undefined; };
console.log('override', Array.from(overridden).join(','));

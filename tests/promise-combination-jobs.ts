export {};
const order: string[] = [];
queueMicrotask(() => order.push('before'));
const settled = [Promise.resolve(1), Promise.reject('two'), Promise.resolve(3)];
const all = Promise.all(settled).catch(reason => {order.push('all:' + reason);});
const statuses = Promise.allSettled(settled).then(values => {
    order.push('settled:' + values.map(value => value.status === 'fulfilled' ? value.value : value.reason).join(','));
});
const race = Promise.race(settled).then(value => {order.push('race:' + value);});
const any = Promise.any(settled).then(value => {order.push('any:' + value);});
const rejected = Promise.any([Promise.reject('first'), Promise.reject('second')]).catch(error => {
    order.push('errors:' + error.errors.join(','));
});
let resolvePending!: (value: number) => void;
const pending = new Promise<number>(resolve => {resolvePending = resolve;});
const mixed = Promise.all([Promise.resolve(4), pending, 6]).then(values => {order.push('mixed:' + values.join(','));});
queueMicrotask(() => {order.push('finish'); resolvePending(5);});
queueMicrotask(() => order.push('after'));
Promise.all([all, statuses, race, any, rejected, mixed]).then(() => console.log('order', order.join('|')));

const descriptor = Object.getOwnPropertyDescriptor(Promise, Symbol.species)!;
let speciesReads = 0, constructions = 0, completions = 0;
class Discarded {
    constructor(executor: any) {constructions++; executor(() => {completions++;}, () => {completions++;});}
}
Object.defineProperty(Promise, Symbol.species, {configurable: true, get(){speciesReads++; return Discarded;}});
const custom = Promise.all([Promise.resolve(7), Promise.resolve(8)]);
Object.defineProperty(Promise, Symbol.species, descriptor);
custom.then(values => console.log('custom species', values.join(','), speciesReads, constructions, completions));

let settleDuringSpecies!: (value: number) => void;
const changed = new Promise<number>(resolve => {settleDuringSpecies = resolve;});
// A species lookup can settle a promise before its reaction is registered.
const constructor = {get [Symbol.species](){settleDuringSpecies(9); return Promise;}};
let constructorReads = 0;
Object.defineProperty(changed, 'constructor', {get(){return ++constructorReads === 1 ? Promise : constructor;}, configurable: true});
Promise.all([changed]).then(values => console.log('settled during species', values.join(','), constructorReads));

const overridden = Promise.resolve(10); let calls = 0;
overridden.then = function(fulfill: any): any {calls++; fulfill(11); fulfill(12);};
Promise.all([overridden]).then(values => console.log('custom then', values.join(','), calls));

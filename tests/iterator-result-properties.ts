function* entries(): Generator<number, number, unknown> { yield 7; return 9; }
const iterator = entries();
const first: any = iterator.next();
const second: any = iterator.next();
console.log(first !== second, JSON.stringify(first), JSON.stringify(second));
console.log(Object.getPrototypeOf(first) === Object.prototype, Object.keys(first).join(','));
console.log(JSON.stringify(Object.getOwnPropertyDescriptors(first)));
first.value = 11;
delete first.done;
first.extra = 13;
first.done = true;
console.log(JSON.stringify(first), JSON.stringify(second));
Object.defineProperty(first, 'value', { get() { return 17; }, enumerable: true, configurable: true });
console.log(JSON.stringify(first));
Object.freeze(second);
console.log(Object.isFrozen(second), JSON.stringify(second));
const third: any = iterator.next();
console.log(Object.keys(third).join(','), third.done, third.value === undefined);
console.log(JSON.stringify(Object.getOwnPropertyDescriptor(third, 'value')));

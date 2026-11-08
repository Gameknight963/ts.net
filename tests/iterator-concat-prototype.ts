/// <reference lib="esnext.iterator" />
export {};
// Older Node versions provide the same helper prototype through flatMap.
const concatenate = (Iterator as any).concat ?? ((...items: number[][]) => Iterator.from(items).flatMap(value => value));
const concat: any = concatenate([1], [2]);
const mapped: any = Iterator.from([3, 4]).map(value => value);
const prototype = Object.getPrototypeOf(mapped);
console.log('prototype', Object.getPrototypeOf(concat) === prototype, Object.prototype.toString.call(concat));
console.log('next identities', concat.next === mapped.next, concat.return === mapped.return);
console.log('borrow concat', JSON.stringify(prototype.next.call(concat)));
console.log('borrow map', JSON.stringify(Object.getPrototypeOf(concat).next.call(mapped)));
console.log('close concat', JSON.stringify(prototype.return.call(concat)), JSON.stringify(concat.next()));
console.log('close map', JSON.stringify(Object.getPrototypeOf(concat).return.call(mapped)), JSON.stringify(mapped.next()));

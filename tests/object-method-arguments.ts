export {};
function create(prefix: string) {
    return {
        total: 0,
        step(): void {this.total++;},
        add(value: number): void {this.total += value;},
        pair(first: number, second: number): number {this.total += first + second; return this.total;},
        text(value: string): string {return prefix + value;},
        defaults(value = 'default'): string {return value;},
        rest(...values: string[]): string {return values.join(',');},
        observed(value: string): string {return value + ':' + arguments.length;},
        captured(): () => number {return () => arguments.length;}
    };
}
type Target = ReturnType<typeof create>;
function send(target: Target, value: number): void {target.step(); target.add(value);}
export function work(count: number): number {
    const target = create('prefix:');
    for (let i = 0; i < count; i++) send(target, i);
    return target.total;
}
const target = create('captured:');
console.log('work', work(64));
console.log('methods', target.step(), target.add(2), target.pair(3, 4), target.text('value'));
console.log('fallback', target.defaults(), target.rest('a', 'b'), target.observed('x'), target.captured()());
const borrowed = {total: 10};
console.log('borrowed', target.add.call(borrowed as any, 3), borrowed.total);
const proxy = new Proxy(target, {get(object, key, receiver) {
    console.log('get', String(key)); return Reflect.get(object, key, receiver);
}});
send(proxy, 2);
console.log('proxy', target.total);
const original = target.text;
function replace(): string {target.text = value => 'new:' + value; return 'argument';}
console.log('replacement', target.text(replace()), target.text('next'));
target.text = original;
const base = {text(value: string): string {return 'base:' + value;}};
const derived = {text(value: string): string {return super.text(value) + ':derived';}};
Object.setPrototypeOf(derived, base);
console.log('super', derived.text('value'));

let methodReads = 0;
Object.defineProperty(target, 'add', {get() {
    methodReads++;
    return function(this: Target, value: number): void {this.total += value * 2;};
}, configurable: true});
send(target, 3);
console.log('method getter', target.total, methodReads);
const inherited = Object.create(target) as Target;
send(inherited, 4);
console.log('inherited method', inherited.total, target.total, methodReads);
const forwarded = new Proxy(target, {get(object, key, receiver) {
    console.log('lookup proxy', String(key)); return Reflect.get(object, key, receiver);
}});
send(forwarded, 5);
console.log('forwarded', target.total, methodReads);

const wide = {
    p0: 0, p1: 1, p2: 2, p3: 3, p4: 4, p5: 5, p6: 6, p7: 7,
    p8: 8, p9: 9, p10: 10, p11: 11, p12: 12, p13: 13, p14: 14,
    total: 0,
    add(value: number): void {this.total += value;}
};
function wideSend(target: {add(value: number): void}, value: number): void {target.add(value);}
for (let i = 0; i < 64; i++) wideSend(wide, i);
console.log('wide method', wide.total, wide.p0, wide.p14);
const wideBorrowed = {total: 10};
wide.add.call(wideBorrowed as any, 5);
console.log('wide borrowed', wideBorrowed.total);

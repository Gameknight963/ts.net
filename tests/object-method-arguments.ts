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

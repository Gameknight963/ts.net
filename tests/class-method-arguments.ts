export {};
class Writer {
    total = 0;
    last = '';
    write(value: number): void {this.total += value;}
    text(value: string): string {this.last = value; return this.last;}
    identity(value: object): object {return value;}
    defaults(value = 'default'): string {return value;}
    rest(...values: string[]): string {return values.join(',');}
    observed(value: string): string {return value + ':' + arguments.length;}
    captured(value: string): () => string {return () => value + ':' + arguments.length;}
}
function send(target: Writer, value: number): void {target.write(value);}
function echo(target: Writer, value: string): string {return target.text(value);}
export function work(count: number): number {
    const target = new Writer();
    for (let i = 0; i < count; i++) {send(target, i); echo(target, 'same');}
    return target.total;
}
console.log('work', work(10));
const writer = new Writer();
console.log('void', send(writer, 3), writer.total);
console.log('text', echo(writer, 'hello'));
const item = {};
console.log('identity', writer.identity(item) === item);
console.log('fallbacks', writer.defaults(), writer.rest('a', 'b'), writer.observed('x'));
console.log('captured arguments', writer.captured('arrow')());
const borrowed = {last: ''};
console.log('borrowed', writer.text.call(borrowed as Writer, 'borrowed'), borrowed.last);
class Derived extends Writer {text(value: string): string {return super.text(value) + ':derived';}}
console.log('super', new Derived().text('base'));
let reads = 0;
const proxy = new Proxy(writer, {get(target, key, receiver) {reads++; return Reflect.get(target, key, receiver);}});
console.log('proxy', echo(proxy, 'proxied'), reads);
const original = writer.text;
function replace(): string {writer.text = value => 'new:' + value; return 'argument';}
console.log('captured', writer.text(replace()), echo(writer, 'next'));
writer.text = original;
class StaticWriter {static prefix = 'static:'; static text(value: string): string {return this.prefix + value;}}
console.log('static', StaticWriter.text('value'));
function closure(prefix: string): (value: string) => string {
    class Captured {text(value: string): string {return prefix + value;}}
    const target = new Captured();
    return value => target.text(value);
}
console.log('closure', closure('captured:')('value'));

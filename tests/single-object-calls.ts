export {};
function invoke(callback: (value: string) => string, value: string): string { return callback(value); }
function echo(value = 'default'): string { return value; }
console.log(invoke(echo, 'hello'), invoke(echo, undefined as any));
function closure(prefix: string): (value: string) => string { return value => prefix + value; }
console.log(invoke(closure('captured:'), 'value'));
function count(value: string): string { return value + ':' + arguments.length; }
console.log(invoke(count, 'one'));
function rest(...values: string[]): string { return values.join(','); }
console.log(invoke(rest, 'rest'));
function destructured({text}: {text: string}): string { return text; }
const destructuredCall: (value: {text: string}) => string = destructured;
console.log(destructuredCall({text: 'object'}));
class Base {
    prefix = 'base:';
    echo(value: string): string { return this.prefix + value; }
}
class Derived extends Base {
    echo(value: string): string { return super.echo(value) + ':derived'; }
}
console.log(new Derived().echo('receiver'));
const receiver = {prefix: 'object:', echo(value: string) { return this.prefix + value; }};
console.log(receiver.echo('receiver'));
const holder = {echo};
function replace(): string { holder.echo = value => 'new:' + value; return 'argument'; }
console.log(holder.echo(replace()), holder.echo('next'));
const target = new Proxy(echo, {apply(fn, receiver, argumentsList) {
    console.log('proxy', receiver === undefined, argumentsList.length);
    return Reflect.apply(fn, receiver, argumentsList);
}});
console.log(invoke(target, 'proxy value'));
const optional: (value: unknown) => unknown = value => value;
console.log(optional(undefined), optional(null), (optional({text: 'returned'}) as {text: string}).text);
console.log('abcdef'.slice('2' as any), 'abc'.includes('b'), [1, 2, 3].at('1' as any));
function work(): number {
    const prefix = '';
    const holder = {echo: (value: string): string => prefix + value};
    let sum = 0;
    for (let i = 0; i < 100000; i++) {
        sum += holder.echo('hello').length;
        sum += invoke(holder.echo, 'hello').length;
    }
    return sum;
}
console.log(work());

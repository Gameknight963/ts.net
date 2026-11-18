export {};
class Echo {
    echo(value: string): string { return 'original:' + value; }
    second(value: string): string { return 'second:' + value; }
    third(value: string): string { return 'third:' + value; }
    fourth(value: string): string { return 'fourth:' + value; }
}
function read(value: Echo): string { return value.echo('value'); }
const value = new Echo();
console.log(read(value), value.second('value'), value.third('value'), value.fourth('value'));
Echo.prototype.echo = text => 'replacement:' + text;
console.log(read(value));
value.echo = text => 'own:' + text;
console.log(read(value));
delete (value as any).echo;
console.log(read(value));
Object.defineProperty(Echo.prototype, 'echo', {configurable: true, get() {
    console.log('getter'); return (text: string) => 'getter:' + text;
}});
console.log(read(value));
Object.defineProperty(Echo.prototype, 'echo', {configurable: true, writable: true, value: (text: string) => 'restored:' + text});
const other = new Echo();
Object.setPrototypeOf(value, {echo: (text: string) => 'prototype:' + text});
console.log(read(value), read(other));
const proxy = new Proxy(other, {get(target, key, receiver) {
    console.log('proxy', String(key)); return Reflect.get(target, key, receiver);
}});
console.log(read(proxy));
class WithGetter {
    get preceding(): string { return 'getter'; }
    echo(text: string): string { return text; }
}
console.log(new WithGetter().echo('after getter'));
function factory(prefix: string) {
    return class Local { echo(text: string): string { return prefix + text; } };
}
const First = factory('first:'); const Second = factory('second:');
console.log(new First().echo('value'), new Second().echo('value'));
function replaceDuringArgument(): string { other.echo = text => 'argument replacement:' + text; return 'argument'; }
console.log(other.echo(replaceDuringArgument()), other.echo('next'));

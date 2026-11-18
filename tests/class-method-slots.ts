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

class Numeric {
    stamp = 1;
    echo(text: string): string {return 'numeric:' + text;}
}
function readNumeric(target: Numeric): string {return target.echo('value');}
const numeric = new Numeric();
console.log('typed', readNumeric(numeric));
class FieldImplementation {stamp = 2; echo = (text: string) => 'field:' + text;}
console.log('different layout', readNumeric(new FieldImplementation()));
class DerivedNumeric extends Numeric {echo = (text: string) => 'derived field:' + text;}
console.log('derived layout', readNumeric(new DerivedNumeric()));
console.log('structural layout', readNumeric({stamp: 3, echo: text => 'literal:' + text}));
console.log('frozen layout', readNumeric(Object.freeze(new Numeric())));
numeric.echo = text => 'own numeric:' + text;
console.log('typed own', readNumeric(numeric));
delete (numeric as any).echo;
console.log('typed restored', readNumeric(numeric));
const numericProxy = new Proxy(numeric, {get(target, key, receiver) {
    console.log('typed proxy', String(key)); return Reflect.get(target, key, receiver);
}});
console.log(readNumeric(numericProxy));
Object.defineProperty(Numeric.prototype, 'echo', {configurable: true, get() {
    return function(this: unknown, text: string) {return (this === numeric ? 'receiver:' : 'other:') + text;};
}});
console.log('typed getter', readNumeric(numeric));
Object.setPrototypeOf(numeric, {echo: (text: string) => 'changed:' + text});
console.log('typed prototype', readNumeric(numeric));

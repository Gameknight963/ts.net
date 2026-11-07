const events: string[] = [];
function mark(label: string): any {
 events.push('eval ' + label);
 return (...args: any[]) => { events.push('call ' + label + ' ' + args.length + ' ' + String(args[1]) + ' ' + (typeof args[2] === 'number' ? args[2] : typeof args[2])); };
}
function double(target: any, key: string | symbol, descriptor: PropertyDescriptor): void {
 const original = descriptor.value;
 descriptor.value = function(this: any, ...args: any[]) { return original.apply(this, args) * 2; };
}
function computed(): string { events.push('key'); return 'computed'; }
@mark('class')
class Example {
 @mark('static') static value = (events.push('static init'), 1);
 @mark('field') field = 2;
 @mark('method') @double method(@mark('p0') x: number, @mark('p1') y: number) { return this.field + x + y; }
 @double [computed()]() { return this.field; }
 constructor(@mark('ctor') public seed: number) {}
}
const value = new Example(7);
console.log(events.join('|'));
console.log(value.method(3, 4), (value as any).computed(), value.seed, Example.value);
function accessor(target: any, key: string, descriptor: PropertyDescriptor): void {
 const original = descriptor.get!;
 descriptor.get = function(this: any) { return original.call(this) + 10; };
}
class Accessor { private stored = 2; @accessor get value() { return this.stored; } set value(n: number) { this.stored = n; } }
const a = new Accessor(); console.log(a.value); a.value = 5; console.log(a.value);
function replace<T extends {new(...args: any[]): any}>(value: T): T { return class extends value { extra = 9; }; }
@replace
class Replaced { self() { return Replaced; } static self() { return Replaced; } field = 3; }
const r: any = new Replaced(); console.log(r.field, r.extra, r instanceof Replaced, r.self() === Replaced, Replaced.self() === Replaced);
function property(target: any, key: string): void { target['marked_' + key] = 'yes'; }
class Properties { @property field = 4; }
console.log((new Properties() as any).marked_field);

function returned(target: any, key: string, descriptor: PropertyDescriptor): PropertyDescriptor { return { value: () => 12 }; }
class Returned { @returned method() { return 3; } }
console.log(new Returned().method());
class StaticWrapper {
 static factor = 3;
 @double static method(value: number) { return this.factor * value; }
}
console.log(StaticWrapper.method(4));
function factory(seed: number) {
 class Local { value = seed; @double method() { return this.value; } }
 return Local;
}
const First = factory(3); const Second = factory(5);
console.log(new First().method(), new Second().method());

const symbolKey = Symbol('method');
function symbolDecorator(target: any, key: string | symbol, descriptor: PropertyDescriptor) {
 console.log(typeof key, key === symbolKey); double(target, key, descriptor);
}
class Symbols { @symbolDecorator [symbolKey]() { return 7; } }
console.log(new Symbols()[symbolKey]());

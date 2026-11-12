class Root {
    value = 3;
    read(): number { return this.value; }
    get doubled(): number { return this.value * 2; }
}
class Child extends Root { override read(): number { return super.read() + 1; } }
const instance = new Child();
console.log(instance.read(), instance.doubled, Object.hasOwn(instance, 'value'));
console.log(Reflect.get(instance, 'doubled', {value: 7}));
const borrowed = {value: 11, read: instance.read};
console.log(borrowed.read());
Object.defineProperty(instance, 'value', {get() {console.log('own getter', this === instance); return 13;}, configurable: true});
console.log(instance.read(), instance.doubled);
delete (instance as any).value;
Object.defineProperty(Root.prototype, 'value', {get() {console.log('prototype getter', this === instance); return 17;}, configurable: true});
console.log(instance.read(), instance.doubled);
const key = Symbol('read');
class Symbolic {value = 19; [key](): number {return this.value;}}
const symbolic = new Symbolic();
console.log(symbolic[key]());
Object.setPrototypeOf(symbolic, {[key](this: any): number {return this.value + 1;}});
console.log(symbolic[key]());
Object.defineProperty(Child.prototype, 'read', {get() {console.log('method getter', this === instance); return () => 23;}, configurable: true});
console.log(instance.read());

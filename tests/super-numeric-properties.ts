export {};
class Base {base = 3; get value(): number {return this.base;} static base = 5; static get value(): number {return this.base;}}
class Child extends Base {
    override get value(): number {return super.value + 1;}
    read(): number {return super.value;}
    arrow(): () => number {return () => super.value;}
    default(value: number = super.value): number {return value;}
    static override get value(): number {return super.value + 1;}
    static read(): number {return super.value;}
}
const child = new Child();
console.log('initial', child.value, child.read(), child.arrow()(), child.default(), Child.value, Child.read());
console.log('borrow', child.read.call({base: 11}), Child.read.call({base: 13}));
const arrow = child.arrow();
Object.setPrototypeOf(Child.prototype, {get value(): number {return (this as any).base * 10;}});
console.log('changed', child.value, child.read(), arrow());
Object.setPrototypeOf(Child, {get value(): number {return (this as any).base * 10;}});
console.log('static changed', Child.value, Child.read());
Object.setPrototypeOf(Child.prototype, {get value(): number {throw new Error('getter');}});
try {child.read();} catch (error) {console.log('thrown', (error as Error).message);}
Object.setPrototypeOf(Child.prototype, null);
try {child.read();} catch {console.log('null prototype');}
const home = {__proto__: {value: 17}, read(): number {return super.value;}};
console.log('object', home.read());
Object.setPrototypeOf(home, {value: undefined});
console.log('undefined', home.read());

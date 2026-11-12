export {};
class Base {static value = 3; static get doubled(): number {return (this as any).value * 2;}}
class Child extends Base {static read(): number {return super.value;}}
console.log('initial', Child.value, Child.doubled, Child.read(), 'value' in Child);
const replacement = {value: 7, get doubled(): number {return (this as any).value * 3;}, set write(value: number) {(this as any).written = value;}};
Object.setPrototypeOf(Child, replacement);
console.log('changed', Child.value, Child.doubled, Child.read(), 'value' in Child);
(Child as any).write = 11;
console.log('setter', (Child as any).written, Object.hasOwn(Child, 'write'), Object.hasOwn(replacement, 'written'));
Object.setPrototypeOf(Child, {});
console.log('removed', Child.value, Child.doubled, Child.read(), 'value' in Child);
Object.setPrototypeOf(Child, null);
console.log('null', Child.value, Child.doubled, 'value' in Child);
try {Child.read();} catch {console.log('null super');}

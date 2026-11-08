export {};
class Parent {static value = 1;}
class Child extends Parent {}
const first: any = Child.bind(null);
const replacement = {value: 2, get label() {return (this as any).name;}};
Object.setPrototypeOf(replacement, Object.getPrototypeOf(Parent));
Object.setPrototypeOf(Child, replacement);
const second: any = Child.bind(null);
console.log('capture', first.value, second.value, second.label, Object.getPrototypeOf(second) === replacement);
const third = {value: 3, get label() {return (this as any).name;}};
Object.setPrototypeOf(first, third);
console.log('changed', first.value, first.label, Object.getPrototypeOf(first) === third);
Object.setPrototypeOf(second, null);
console.log('null', second.value, second.label, 'value' in second);

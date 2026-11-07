function plain(value = 1) { return value + 1; }
const arrow = (value = 2) => value * 2;
const object = { method(value = 3) { return value; }, get value() { return 4; } };
async function asyncValue() { return 5; }
function* generator() { yield 6; }
async function* asyncGenerator() { yield 7; }
class Example { method() { return 8; } }
function compact(value:string):string{return value;}
console.log(plain.toString().includes('return value + 1'), arrow.toString().includes('=>'));
console.log(object.method.toString().includes('return value'), Object.getOwnPropertyDescriptor(object,'value')!.get!.toString().includes('return 4'));
console.log(asyncValue.toString().includes('async function'), generator.toString().includes('function*'), asyncGenerator.toString().includes('async function*'));
console.log(Example.toString().includes('class Example'), Example.prototype.method.toString().includes('return 8'));
console.log(plain.bind(null).toString().includes('[native code]'), Math.max.toString().includes('[native code]'));
console.log(compact.toString().includes('return value;'));
function factory(value=1) { return () => value; }
const first=factory(1), second=factory(2);console.log(first.toString()===second.toString(),first.toString().includes('value'));
function comments() { /* retained comment */ return 'ok'; } console.log(comments.toString().includes('retained comment'));


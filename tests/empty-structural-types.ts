interface Empty {}
type EmptyAlias = {};
const first = {};
const inferred = [first, 1, 'text', true];
for (const value of inferred) console.log('inferred', typeof value, value === first);
const explicit: Empty[] = [2, 'hello', false, {}, () => 3, [4]];
for (const value of explicit) console.log('explicit', typeof value);
function identity<T extends {}>(value: T): T {return value;}
function accept(value: EmptyAlias): Empty {return value;}
console.log('calls', identity(5), accept('word'), accept(false));
const fields: {value: Empty} = {value: 6};
console.log('field', fields.value);
fields.value = 'changed';
console.log('updated', fields.value);
const dynamic: any = fields;
dynamic.value = true;
console.log('dynamic', fields.value);
const array: Empty[] = [7];
array.push('eight');
console.log('array', array[0], array[1]);
const alias: EmptyAlias = 9;
const {value = alias} = {} as {value?: Empty};
console.log('default', value);
class EmptyClass {}
const instance = new EmptyClass();
console.log('class', instance instanceof EmptyClass);
const emptyClassValues: EmptyClass[] = [10, 'class-compatible', instance];
for (const item of emptyClassValues) console.log('class type', typeof item, item instanceof EmptyClass);

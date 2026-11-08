export {};
const values = [1, 2, 3];
Object.defineProperty(values, 'length', {value: 2, writable: false});
console.log('length', values.length, values.join(','), JSON.stringify(Object.getOwnPropertyDescriptor(values, 'length')), Object.keys(values).join(','), Object.keys(values).length);
values[0] = 7;
console.log('existing write', values.join(','));
try {values[2] = 9;} catch (error) {console.log('grow', (error as Error).name);}
try {values.push(9);} catch (error) {console.log('push', (error as Error).name);}
try {values.length = 2;} catch (error) {console.log('same assignment', (error as Error).name);}
console.log('same definition', Reflect.defineProperty(values, 'length', {value: 2}), Reflect.defineProperty(values, 'length', {writable: true}));
try {values.pop();} catch (error) {console.log('pop', (error as Error).name, values.length, Object.hasOwn(values, '1'), values.join(','));}
const empty: number[] = [];
Object.defineProperty(empty, 'length', {writable: false});
try {empty.pop();} catch (error) {console.log('empty pop', (error as Error).name);}
try {empty.push();} catch (error) {console.log('empty push', (error as Error).name);}
const mixed: any[] = [1, 'two'];
Object.defineProperty(mixed, 'length', {writable: false});
mixed[0] = 'one';
try {mixed[2] = 3;} catch (error) {console.log('mixed grow', (error as Error).name, mixed.join(','));}
let reads = 0;
const converted = [1, 2, 3];
Object.defineProperty(converted, 'length', {value: {valueOf() {reads++; return 1;}}});
console.log('conversion', reads, converted.length, converted.join(','));
for (const descriptor of [{configurable: true}, {enumerable: true}, {get() {return 1;}}, {value: -1}, {value: 1.5}]) {
    try {Object.defineProperty(converted, 'length', descriptor);} catch (error) {console.log('invalid', (error as Error).name);}
}
for (const operation of ['shift', 'unshift', 'splice']) {
    const item = [1, 2, 3];
    Object.defineProperty(item, 'length', {writable: false});
    try {
        if (operation === 'shift') item.shift();
        else if (operation === 'unshift') item.unshift(0);
        else item.splice(1, 1);
    } catch (error) {console.log('mutation', operation, (error as Error).name, item.length, item.join(','));}
}
const objects = [{value: 1}];
Object.defineProperty(objects, 'length', {writable: false});
objects[0] = {value: 2};
try {objects.push({value: 3});} catch (error) {console.log('object growth', (error as Error).name, objects[0].value, objects.length);}
const reverse: any[] = ['a', 'b'];
reverse.reverse();
Object.defineProperty(reverse, 'length', {writable: false});
reverse[0] = 'c';
try {reverse.push('d');} catch (error) {console.log('reverse growth', (error as Error).name, reverse.join(','));}
let changes = 0;
try {Object.defineProperty(converted, 'length', {value: {valueOf() {return ++changes;}}, configurable: true});} catch (error) {console.log('changed conversion', (error as Error).name, changes, converted.length);}
function template(parts: TemplateStringsArray) {
    console.log('template length', Reflect.defineProperty(parts, 'length', {value: parts.length}), Reflect.defineProperty(parts, 'length', {writable: true}));
}
template`value`;

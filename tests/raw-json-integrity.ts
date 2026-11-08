export {};
const raw: any = (JSON as any).rawJSON('123');
console.log('integrity', Object.isExtensible(raw), Object.isSealed(raw), Object.isFrozen(raw));
console.log('identity', Object.preventExtensions(raw) === raw, Object.seal(raw) === raw, Object.freeze(raw) === raw);
console.log('compatible', Reflect.defineProperty(raw, 'rawJSON', {}), Reflect.defineProperty(raw, 'rawJSON', {value: '123'}), Reflect.defineProperty(raw, 'rawJSON', {writable: false, configurable: false, enumerable: true}));
console.log('incompatible', Reflect.defineProperty(raw, 'rawJSON', {value: '456'}), Reflect.defineProperty(raw, 'rawJSON', {writable: true}), Reflect.defineProperty(raw, 'rawJSON', {enumerable: false}), Reflect.defineProperty(raw, 'extra', {value: 1}));
console.log('reflect', Reflect.isExtensible(raw), Reflect.preventExtensions(raw), Reflect.set(raw, 'rawJSON', '456'), Reflect.deleteProperty(raw, 'rawJSON'), Reflect.deleteProperty(raw, 'extra'));
console.log('keys', Object.keys(raw).join(','), Object.getOwnPropertyNames(raw).join(','), JSON.stringify(raw));
console.log('accessor', Reflect.defineProperty(raw, 'rawJSON', {get: undefined}), Reflect.defineProperty(raw, 'rawJSON', {set: undefined}));
console.log('object definition', Object.defineProperty(raw, 'rawJSON', {value: '123'}) === raw, Object.defineProperties(raw, {rawJSON: {enumerable: true}}) === raw);
for (const attributes of [{value: '456'}, {get() {return '123';}}, {configurable: true}]) {
    try {Object.defineProperty(raw, 'rawJSON', attributes); console.log('unexpected');}
    catch (error) {console.log('rejected', (error as Error).name);}
}

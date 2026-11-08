export {};
for (const value of [JSON, Reflect]) {
    const descriptor = Object.getOwnPropertyDescriptor(value, Symbol.toStringTag)!;
    const child = Object.create(value);
    console.log('tag', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable, Object.prototype.toString.call(value), Object.prototype.toString.call(child));
    console.log('delete', Reflect.deleteProperty(value, Symbol.toStringTag), Object.prototype.toString.call(value), Object.prototype.toString.call(child));
    Object.defineProperty(value, Symbol.toStringTag, {value: 7, configurable: true});
    console.log('nonstring', Object.prototype.toString.call(value));
    Object.defineProperty(value, Symbol.toStringTag, {get() {return 'Custom';}, configurable: true});
    console.log('getter', Object.prototype.toString.call(value), Object.prototype.toString.call(child));
    Object.freeze(value);
    console.log('frozen', Object.isFrozen(value), Reflect.deleteProperty(value, Symbol.toStringTag));
}
console.log('methods', JSON.parse('1'), Reflect.get({value: 2}, 'value'));

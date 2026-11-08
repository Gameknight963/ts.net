export {};
for (const symbol of [Symbol('field'), Symbol(), Symbol(''), Symbol.iterator]) {
    const object = {get [symbol]() {return 1;}, set [symbol](value: number) {}};
    const descriptor = Object.getOwnPropertyDescriptor(object, symbol)!;
    console.log('object', descriptor.get!.name, descriptor.set!.name);
    class Value {
        get [symbol]() {return 2;}
        set [symbol](value: number) {}
    }
    const member = Object.getOwnPropertyDescriptor(Value.prototype, symbol)!;
    console.log('class', member.get!.name, member.set!.name);
    const methods = {[symbol]() {return 3;}};
    console.log('method', methods[symbol].name);
}
console.log('intrinsic', Object.getOwnPropertyDescriptor(RegExp, Symbol.species)!.get!.name);

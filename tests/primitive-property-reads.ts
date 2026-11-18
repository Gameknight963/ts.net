export {};
const strings: any = String.prototype, numbers: any = Number.prototype, booleans: any = Boolean.prototype;
const originalSlice = strings.slice, originalParent = Object.getPrototypeOf(strings);
try {
    strings.probe = 7;
    console.log('string data', ('abc' as any).probe);
    strings.probe = null;
    console.log('string null', ('abc' as any).probe);
    strings.probe = undefined;
    console.log('string undefined', ('abc' as any).probe);
    let reads = 0;
    Object.defineProperty(strings, 'probe', {get() {reads++; return typeof this;}, configurable: true});
    console.log('string getter', ('abc' as any).probe, reads);
    numbers.probe = 42;
    console.log('number data', (12 as any).probe);
    Object.defineProperty(numbers, 'probe', {get() {reads++; return typeof this;}, configurable: true});
    console.log('number getter', (12 as any).probe, reads);
    Object.defineProperty(booleans, 'probe', {get() {reads++; return typeof this;}, configurable: true});
    console.log('boolean getter', (true as any).probe, reads);
    const value: any = 'abcdef', key = ['sli', 'ce'].join('');
    console.log('builtin', value[key](5));
    strings.slice = function(this: unknown, index: number): string {return typeof this + ':' + index;};
    console.log('changed method', value[key](2));
    Object.defineProperty(strings, 'slice', {get() {reads++; return originalSlice;}, configurable: true});
    console.log('method getter', value[key](3), reads);
    delete strings.slice;
    const parent = Object.create(originalParent);
    parent.slice = function(this: unknown, index: number): string {return 'inherited:' + typeof this + ':' + index;};
    Object.setPrototypeOf(strings, parent);
    console.log('inherited method', value[key](4));
    console.log('missing', value.missingProperty);
} finally {
    Object.setPrototypeOf(strings, originalParent);
    Object.defineProperty(strings, 'slice', {value: originalSlice, writable: true, configurable: true});
    delete strings.probe; delete numbers.probe; delete booleans.probe;
}
console.log('restored', ('abcdef' as any).slice(5));

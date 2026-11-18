export {};
function sum(value: any, method: any, count: number, seed = 0): number {
    let total = seed;
    for (let i = 0; i < count; i++) {const result = value[method](5) as string; total += result.length;}
    return total;
}
function typed(value: string): number {
    let total = 0;
    for (let i = 0; i < 40; i++) {const result = value.slice(2); total += result.length;}
    return total;
}
console.log('ordinary', sum('abcdef', 'slice', 40), sum('abcdef', 'substring', 40), typed('abcdef'));
console.log('bounds', sum('abcdef', 'slice', 40.5), sum(null, 'slice', 0), sum(null, 'slice', NaN));
console.log('rounding', sum('abcdef', 'slice', 40, 9007199254740992));
console.log('strings', sum('', 'slice', 40), sum('A😀Bcd', 'slice', 40), sum('😀😀😀', 'slice', 40));
function lastResult(value: any, method: any): string {
    let total = 0; var result = 'initial';
    for (let i = 0; i < 40; i++) {var result = value[method](5) as string; total += result.length;}
    return total + ':' + result;
}
console.log('var result', lastResult('abcdef', 'slice'));
function escapingResult(value: any, method: any): string {
    let total = 0;
    for (let i = 0; i < 40; i++) {var result = value[method](5) as string; total += result.length;}
    return total + ':' + result!;
}
console.log('escaping result', escapingResult('abcdef', 'slice'));
const descriptor = Object.getOwnPropertyDescriptor(String.prototype, 'slice')!;
const original = String.prototype.slice;
let reads = 0;
try {
    Object.defineProperty(String.prototype, 'slice', {get(){reads++; return original;}, configurable: true});
    console.log('getter', sum('abcdef', 'slice', 40), reads);
} finally {Object.defineProperty(String.prototype, 'slice', descriptor);}
let changedReads = 0, changedCalls = 0;
try {
    Object.defineProperty(String.prototype, 'slice', {get(){
        changedReads++;
        Object.defineProperty(String.prototype, 'slice', {value: function(){changedCalls++; return 'next';}, writable: true, configurable: true});
        return original;
    }, configurable: true});
    console.log('changing getter', sum('abcdef', 'slice', 40), changedReads, changedCalls);
} finally {Object.defineProperty(String.prototype, 'slice', descriptor);}
let calls = 0;
try {
    String.prototype.slice = function(){calls++; return 'custom';};
    console.log('custom method', sum('abcdef', 'slice', 40), calls);
} finally {Object.defineProperty(String.prototype, 'slice', descriptor);}
let keys = 0;
const key = {[Symbol.toPrimitive](){keys++; return 'slice';}};
console.log('key conversion', sum('abcdef', key, 40), keys);
function convertedArgument(value: any, method: any, position: any): number {
    let total = 0;
    for (let i = 0; i < 40; i++) {const result = value[method](position) as string; total += result.length;}
    return total;
}
let argumentConversions = 0;
const position = {[Symbol.toPrimitive](){argumentConversions++; return 5;}};
console.log('argument conversion', convertedArgument('abcdef', 'slice', position), argumentConversions);
function changingBody(): number {
    const value: any = 'abcdef', method: string = 'slice'; let total = 0;
    for (let i = 0; i < 40; i++) {
        const result = value[method](5) as string;
        String.prototype.slice = function(){return 'next';};
        total += result.length;
    }
    return total;
}
try {console.log('body mutation', changingBody());} finally {Object.defineProperty(String.prototype, 'slice', descriptor);}
let conversions = 0;
const boxed = new String('abcdef');
Object.defineProperty(boxed, Symbol.toPrimitive, {value(){conversions++; return 'abcdef';}, configurable: true});
console.log('boxed', sum(boxed, 'slice', 40), conversions);
try {
    Object.defineProperty(String.prototype, '__rangeAlias', {value: String.prototype.substring, configurable: true});
    console.log('own alias', sum('abcdef', '__rangeAlias', 40));
    Object.defineProperty(Object.prototype, '__inheritedRange', {value: original, configurable: true});
    console.log('inherited alias', sum('abcdef', '__inheritedRange', 40));
} finally {delete (String.prototype as any).__rangeAlias; delete (Object.prototype as any).__inheritedRange;}
try {
    Object.defineProperty(String.prototype, '0', {value: original, configurable: true});
    let threw = false;
    try {sum('abcdef', '0', 40);} catch {threw = true;}
    console.log('indexed shadow', threw, sum('', '0', 40));
} finally {delete (String.prototype as any)['0'];}
let lengthThrew = false;
try {sum('abcdef', 'length', 40);} catch {lengthThrew = true;}
console.log('length shadow', lengthThrew);
try {
    Object.defineProperty(Number.prototype, '__rangeAlias', {value: original, configurable: true});
    console.log('number receiver', sum(123456, '__rangeAlias', 40));
} finally {delete (Number.prototype as any).__rangeAlias;}

const values: any[] = [undefined, null, true, false, '', ' ', '1', 'NaN', 'Infinity', '1tail', NaN, Infinity, -Infinity, -0, 1, [], [1], [1, 2], {}, Object(2)];
for (let index = 0; index < values.length; index++) {
    const value = values[index];
    console.log(index, isNaN(value), isFinite(value), Number.isNaN(value), Number.isFinite(value));
}
const value = {valueOf(): number {console.log('valueOf');return 3;}, toString(): string {throw 'unexpected string conversion';}};
console.log('coerced', isNaN(value as any), isFinite(value as any));
console.log('not coerced', Number.isNaN(value), Number.isFinite(value));
const fallback = {valueOf(): any {console.log('object valueOf');return {};}, toString(): string {console.log('string fallback');return '4';}};
console.log('fallback', isNaN(fallback as any), isFinite(fallback as any));
const broken = {valueOf(): never {throw 'conversion failure';}};
for (const method of [isNaN, isFinite]) {
    try {method(broken as any);} catch(error) {console.log('failure', error);}
    console.log('metadata', method.name, method.length);
}
console.log('missing', (isNaN as any)(), (isFinite as any)());
console.log('distinct', isNaN === Number.isNaN, isFinite === Number.isFinite);
const alias = isNaN; console.log('alias', alias('invalid' as any));
function shadow(isNaN: (value: number) => string): string {return isNaN(7);}
console.log('shadow', shadow(value => 'local ' + value));

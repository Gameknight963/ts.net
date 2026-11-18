export {};
function slice(value: any, index: number): string {return value.slice(index);}
function dynamic(value: any, name: string, index: number): any {return value[name](index);}
for (const index of [-Infinity, NaN, -0, -8, -1, 0, 0.5, 1, 5, 6, Infinity, undefined]) {
    for (const name of ['slice', 'substring', 'charAt', 'charCodeAt', 'codePointAt', 'at', 'indexOf', 'lastIndexOf', 'includes']) {
        console.log(name, dynamic('abcdefundefined123', name, index as number));
    }
    console.log('slice', slice('abcdef', index as number));
    console.log('array at', dynamic([10, 20, 30], 'at', index as number));
}
const ordinary = {base: 2, run(value: number): number {return this.base + value;}};
console.log('ordinary', dynamic(ordinary, 'run', 3));
const observed = {run(value: number): number {return value + arguments.length;}};
console.log('arguments', dynamic(observed, 'run', 4));
const borrowed = {slice: String.prototype.slice, toString() {console.log('receiver conversion'); return 'abcdef';}};
console.log('borrowed', slice(borrowed, 2));
const throwing = {slice: String.prototype.slice, toString() {throw 'conversion';}};
try {slice(throwing, 2);} catch {console.log('throwing receiver');}
const invalid: any = {run: 1};
try {dynamic(invalid, 'run', 3);} catch {console.log('not callable');}

const replaced: any = [1, 2];
replaced.slice = function(value: number): string {return 'slice:' + value + ':' + arguments.length + ':' + (this === replaced);};
replaced.pop = function(): number {return arguments.length;};
replaced.includes = function(value: string): boolean {return value === 'x' && arguments.length === 1;};
console.log('array overrides', replaced.slice(2), replaced.pop(), replaced.includes('x'));
const captured: any = {slice(value: number): string {return 'old:' + value;}};
function argument(): number {captured.slice = (value: number) => 'new:' + value; return 2;}
console.log('captured method', captured.slice(argument()), captured.slice(3));

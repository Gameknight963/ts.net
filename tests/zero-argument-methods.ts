export {};
for (const text of ['', 'abc', 'undefined', 'A??B']) {
    const value: any = text;
    for (const name of ['slice', 'substring', 'charAt', 'charCodeAt', 'codePointAt', 'at', 'includes', 'startsWith', 'endsWith', 'indexOf', 'lastIndexOf']) {
        console.log(name, value[name]());
    }
}
for (const values of [[], [1, 2], [, 2]]) console.log('array at', (values as any).at());
class Textual extends String {
    first(): string {return (super.charAt as any)();}
    position(): number {return (super.charCodeAt as any)();}
    sliceAll(): string {return super.slice();}
    relative(): string {return (super.at as any)();}
    includesDefault(): boolean {return (super.includes as any)();}
}
const text = new Textual('undefined');
console.log('super defaults', text.first(), text.position(), text.sliceAll(), text.relative(), text.includesDefault());
const ordinary = {value: 3, run(): number {return this.value;}};
console.log('ordinary', ordinary.run());
let reads = 0;
const accessor = {get run() {reads++; return function(this: {value: number}): number {return this.value;};}, value: 4};
console.log('getter method', accessor.run(), reads);
try {({run: 1} as any).run();} catch {console.log('not callable');}
const missing: any = undefined;
try {(String.prototype.charAt as any).call(missing);} catch {console.log('missing receiver');}

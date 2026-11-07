interface String {isWellFormed(): boolean; toWellFormed(): string;}
const pieces = ['a', '\uD7FF', '\uD800', '\uDBFF', '\uDC00', '\uDFFF', '\uE000', '\uD83D\uDE00'];
function report(value: string): void {
    const repaired = value.toWellFormed();
    const units: number[] = [];
    for (let index = 0; index < repaired.length; index++) units.push(repaired.charCodeAt(index));
    console.log(value.isWellFormed(), repaired.isWellFormed(), value.length === repaired.length, units.join(','));
}
report('');
for (const first of pieces) for (const second of pieces) for (const third of pieces) report(first + second + third);
const check = String.prototype.isWellFormed as any;
const repair = String.prototype.toWellFormed as any;
console.log('generic', check.call(123), repair.call(false));
const object = {toString(): string {console.log('convert'); return 'x\uD800';}};
console.log('object', check.call(object), repair.call(object).charCodeAt(1));
for (const method of [check, repair]) {
    try {method.call(null);} catch (error) {console.log('null', (error as Error).name);}
    try {method.call(undefined);} catch (error) {console.log('undefined', (error as Error).name);}
    try {method.call({toString(): never {throw 'coercion';}});} catch (error) {console.log('failure', error);}
    console.log('metadata', method.name, method.length, Object.keys(String.prototype).includes(method.name));
}

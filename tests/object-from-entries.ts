const plain = Object.fromEntries([['a', 1], ['b', 2], ['a', 3]]);
console.log('plain', plain.a, plain.b, Object.keys(plain).join(','));
console.log('prototype', Object.getPrototypeOf(plain) === Object.prototype);
const special = Object.fromEntries([['__proto__', 7], ['constructor', 8], ['toString', 9]]);
console.log('special', special.__proto__, special.constructor, special.toString, Object.getPrototypeOf(special) === Object.prototype);
const map = new Map<string, number>([['x', 10], ['y', 11]]);
console.log('map', Object.fromEntries(map).x, Object.fromEntries(map.entries()).y);
const entries: any[] = [{0: 'missing'}, {1: 'value'}, [true, 12], [null, 13], [undefined, 14]];
const loose = Object.fromEntries(entries);
console.log('loose', loose.missing, loose.undefined, loose.true, loose.null);
const key = {toString(): string {console.log('key'); return 'coerced';}};
const entry = {get 0(): any {console.log('get key'); return key;}, get 1(): number {console.log('get value'); return 15;}};
console.log('order', Object.fromEntries([entry] as any).coerced);
console.log('function', Object.fromEntries.name, Object.fromEntries.length, Object.keys(Object).includes('fromEntries'));
console.log('receiver', Object.fromEntries.call(null, [['z', 16]]).z);
console.log('empty', Object.keys(Object.fromEntries([])).length);
for (const input of [null, undefined, 42, {}, [1], ['ab'], [null]]) {
    try {Object.fromEntries(input as any);} catch (error) {console.log('invalid', (error as Error).name);}
}
const iterator = new Set<any>([entry, 1, ['unused', 17]]).values();
(iterator as any).return = function (): any {console.log('closed', this.next().value[0]); throw 'cleanup error';};
try {Object.fromEntries(iterator);} catch (error) {console.log('entry error', (error as Error).name);}
const badKey = {toString(): never {throw 'key failure';}};
const failing = new Map<any, any>([[badKey, 18]]).entries();
(failing as any).return = function (): any {console.log('key cleanup'); throw 'cleanup failure';};
try {Object.fromEntries(failing);} catch (error) {console.log('original', error);}
function readRecord(record: Record<string, number>): number {return record.a + record.b;}
const typed: Record<string, number> = Object.fromEntries([['a', 20], ['b', 22]]);
console.log('typed', readRecord(typed));
typed.c = 23;
console.log('typed write', typed.c, Object.keys(typed).length);

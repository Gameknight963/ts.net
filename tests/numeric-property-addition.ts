export {};
function add(target: any, value: number): number {return target.base + value;}
for (const target of [{base: 2}, {base: 1.5}, {base: -0}, {base: NaN}, {base: Infinity}])
    console.log(add(target, 3));
console.log(add({base: null}, 3), add({base: false}, 3), add({base: undefined}, 3));
console.log('zero signs', Object.is(add({base: -0}, -0), -0), Object.is(add({base: 0}, -0), -0));
const optional: {base: number | undefined} = {base: 7};
console.log(add(optional, 2)); optional.base = undefined; console.log(add(optional, 2));
let events = '';
const getter = {get base() {events += 'G'; return 5;}};
console.log(add(getter, 3), events);
events = '';
const coercible = {base: {[Symbol.toPrimitive](hint: string) {events += hint; return 7;}}};
console.log(add(coercible, 3), events);
function captured(value: number): number {
    const target: any = {get base() {value = 9; return 2;}};
    return target.base + value;
}
console.log('captured', captured(3));
let globalValue = 3;
const globalTarget: any = {get base() {globalValue = 9; return 2;}};
function globalRead(): number {return globalTarget.base + globalValue;}
console.log('global', globalRead());
function ordered(value: number): number {
    const target: any = {get base() {events += 'G'; return 4;}};
    return target.base + (() => {events += 'R'; return value;})();
}
events = ''; console.log('order', ordered(3), events);
function indexed(target: any, values: number[]): number {return target.base + values[0];}
const moving = {base: 4};
const values = [3];
Object.defineProperty(values, '0', {get() {moving.base = 99; return 5;}});
console.log('saved left', indexed(moving, values), moving.base);
const updated: any = {get base() {values[1] = 9; return 2;}};
function indexedSecond(target: any, values: number[]): number {return target.base + values[1];}
console.log('getter first', indexedSecond(updated, values));
let primitive = 1;
function coerceAfterRight(target: any): number {
    return target.base + (() => {primitive = 9; return 3;})();
}
console.log('coerce after right', coerceAfterRight({base: {valueOf() {return primitive;}}}));

export {};
type Cell = { value: number | undefined };
function add(cell: Cell, right: number): number { return cell.value! + right; }
function multiply(cell: Cell): number { return cell.value! * 1; }
function read(cell: Cell): number | undefined { return cell.value; }
function choose(flag: boolean): number | undefined { return flag ? undefined : 3; }
const first = { value: choose(false), first: true };
const second = { value: choose(true), second: 'second' };
const third = { value: -0, third: true, extra: 7 };
console.log('number', add(first, 4), read(first));
console.log('missing', Number.isNaN(add(second, 4)), read(second) === undefined, String(read(second)));
console.log('predicates', Number.isNaN(second.value), Number.isFinite(second.value), second.value === undefined);
console.log('zero', Object.is(multiply(third), -0), Object.is(read(third), -0));
first.value = undefined;
console.log('written missing', Number.isNaN(add(first, 4)), read(first) === undefined);
first.value = NaN;
console.log('written nan', Number.isNaN(add(first, 4)), Number.isNaN(read(first)), read(first) === undefined);
first.value = Infinity;
console.log('infinity', add(first, 4), multiply(first));
first.value = -Infinity;
console.log('negative infinity', add(first, 4), multiply(first));
first.value = 11;
console.log('updated', add(first, 4), read(first));
const log: string[] = [];
const getter = { get value(): number { log.push('get'); return 13; } };
console.log('getter', add(getter, 4), log.join(','));
function right(): number { log.push('right'); return 4; }
function ordered(cell: Cell): number { return cell.value! + right(); }
log.length = 0;
console.log('order', ordered(getter), log.join(','));
const inherited: Cell = Object.create({ get value() { return (this as any).other; } });
(inherited as any).other = 17;
console.log('inherited', add(inherited, 4));
console.log('serialized', JSON.stringify(second));

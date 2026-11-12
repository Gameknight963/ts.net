export {};

function make(): (number | string | undefined)[] {
    const values: (number | string | undefined)[] = [1, 'two', , 4];
    if (typeof values[0] !== 'number') throw new Error('wrong initial value');
    return values;
}

const sealed = make();
Object.seal(sealed);
sealed[0] = NaN;
sealed[1] = -0;
console.log('sealed existing writes', Number.isNaN(sealed[0]), Object.is(sealed[1], -0));
try {sealed[2] = 8;} catch {console.log('sealed hole rejected');}

const locked = make();
Object.defineProperty(locked, 'length', {writable: false});
locked[0] = 7;
console.log('locked length existing write', locked[0], locked.length);
try {locked[4] = 8;} catch {console.log('locked length growth rejected');}

let inherited = 0;
const custom = make();
Object.setPrototypeOf(custom, {
    set 0(value: number) {throw new Error('own write invoked inherited setter');},
    get 2() {return inherited;},
    set 2(value: number) {inherited = value;}
});
custom[0] = 9;
custom[2] = 10;
console.log('custom prototype writes', custom[0], custom[2], Object.hasOwn(custom, '2'));

let assigned = 0;
const defined = make();
Object.defineProperty(defined, '0', {get() {return assigned;}, set(value: number) {assigned = value;}, configurable: true});
defined[0] = 11;
Object.defineProperty(defined, '1', {value: 'fixed', writable: false});
console.log('defined setter write', defined[0]);
try {defined[1] = 12;} catch {console.log('defined readonly write rejected');}

const frozen = make();
Object.freeze(frozen);
try {frozen[0] = 13;} catch {console.log('frozen existing write rejected');}

function store(values: (number | string | undefined)[], value: number | undefined): void {values[0] = value;}
const optional = make();
store(optional, undefined);
console.log('undefined numeric write', optional[0], typeof optional[0] === 'number');
store(optional, 14);
console.log('defined numeric write', optional[0]);

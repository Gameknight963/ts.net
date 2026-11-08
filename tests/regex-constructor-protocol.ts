export {};
let effects = '';
const pattern: any = {
    get [Symbol.match]() { effects += 'M'; return true; },
    get constructor() { effects += 'C'; return undefined; },
    get source() { effects += 'S'; return {toString() {effects += 's'; return 'a';}}; },
    get flags() { effects += 'F'; return {toString() {effects += 'f'; return 'gi';}}; },
    toString() { effects += 'T'; return 'text'; }
};
function show(label: string, make: () => RegExp): void {
    effects = '';
    const result = make();
    console.log(label, result.source, result.flags, effects);
}
show('call', () => RegExp(pattern));
show('new', () => new RegExp(pattern));
show('flags', () => new RegExp(pattern, 'm'));
const same: any = {[Symbol.match]: true, constructor: RegExp};
console.log('identity', RegExp(same) === same);
const ordinary: any = {[Symbol.match]: false, source: 'wrong', flags: 'g', toString() {return 'right';}};
show('ordinary', () => new RegExp(ordinary));
const native: any = /a/g;
native[Symbol.match] = false;
console.log('native', RegExp(native) === native, new RegExp(native).source, new RegExp(native).flags);
const generic: any = {[Symbol.match]: true, source: 'a', flags: 'g', lastIndex: 1};
console.log('matchAll', Array.from(RegExp.prototype[Symbol.matchAll].call(generic, 'aaa'), match => match.index).join(','));
console.log('split', JSON.stringify(RegExp.prototype[Symbol.split].call(generic, 'bab')));
function changing(): RegExp {
    const value = /a/g;
    const prototype = {get [Symbol.match](): any {
        RegExp.prototype.compile.call(value, 'b', 'i');
        return true;
    }};
    Object.setPrototypeOf(prototype, RegExp.prototype);
    Object.setPrototypeOf(value, prototype);
    return value;
}
console.log('changed matchAll', Array.from(RegExp.prototype[Symbol.matchAll].call(changing(), 'abaB'), match => match[0]).join(','));
console.log('changed split', JSON.stringify(RegExp.prototype[Symbol.split].call(changing(), 'abaB')));

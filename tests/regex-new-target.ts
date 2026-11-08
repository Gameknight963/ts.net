export {};
function Alternate() {}
const bound = Alternate.bind(null);
let order = '', text = 'a', throwing = false;
Object.defineProperty(bound, 'prototype', {get() {order += 'prototype|'; text = 'b'; if (throwing) throw new Error('prototype'); return Alternate.prototype;}});
const pattern = {get [Symbol.match]() {order += 'match|'; return true;},
    get source() {order += 'source|'; return {toString() {order += 'text|'; return text;}};},
    get flags() {order += 'flags|'; return {toString() {order += 'flag text|'; return 'i';}};}};
const value = Reflect.construct(RegExp, [pattern], bound);
console.log('pattern', order, RegExp.prototype.test.call(value, 'B'), Object.getPrototypeOf(value) === Alternate.prototype);
order = ''; throwing = true;
try {Reflect.construct(RegExp, [pattern], bound);} catch (error) {console.log('throw', order, (error as Error).message);}
order = ''; throwing = false;
const plain = {get [Symbol.match]() {order += 'match|'; return false;}, toString() {order += 'text|'; return 'c';}};
const ordinary = Reflect.construct(RegExp, [plain, ''], bound);
console.log('ordinary', order, RegExp.prototype.test.call(ordinary, 'c'));
order = '';
try {Reflect.construct(RegExp, ['[', ''], bound);} catch (error) {console.log('syntax', order, (error as Error).name);}
order = '';
const explicit = Reflect.construct(RegExp, [pattern, 'g'], bound);
console.log('explicit flags', order, RegExp.prototype.test.call(explicit, 'b'));
order = '';
const failed = {get [Symbol.match]() {order += 'match|'; return true;}, get source() {order += 'source|'; throw new Error('source');}};
try {Reflect.construct(RegExp, [failed], bound);} catch (error) {console.log('source failure', order, (error as Error).message);}

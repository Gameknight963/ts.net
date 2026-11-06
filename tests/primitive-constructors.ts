console.log('string-call', String(), String(undefined), String(null), String(42), String(false));
console.log('boolean-call', Boolean(), Boolean(undefined), Boolean(null), Boolean(0), Boolean(''), Boolean('false'), Boolean({}));
const text = new String('ab');
const flag = new Boolean(false);
console.log('wrappers', typeof text, typeof flag, text instanceof String, flag instanceof Boolean, text instanceof Object, flag instanceof Object);
console.log('constructors', text.constructor === String, flag.constructor === Boolean, Object('ab').constructor === String, Object(false).constructor === Boolean, Object(1).constructor === Number);
console.log('values', text.valueOf(), text.toString(), flag.valueOf(), flag.toString(), Boolean(flag));
console.log('prototypes', String.prototype.valueOf(), Boolean.prototype.valueOf(), String.prototype.constructor === String, Boolean.prototype.constructor === Boolean, String.prototype instanceof String, Boolean.prototype instanceof Boolean);
console.log('keys', Object.keys(String.prototype).length, Object.keys(Boolean.prototype).length, Object.keys(text).join(','));
console.log('primitive-methods', 'abc'.valueOf(), 'abc'.toString(), false.valueOf(), true.toString());
const stringMethod = 'abc'.valueOf;
console.log('method-value', stringMethod.call('other'));
console.log('prototype-identity', Object.getPrototypeOf(text) === String.prototype, Object.getPrototypeOf('ab') === String.prototype, Object.getPrototypeOf(flag) === Boolean.prototype, Object.getPrototypeOf(false) === Boolean.prototype);
console.log('primitive-prototypes', ('ab' as any).__proto__ === String.prototype, (false as any).__proto__ === Boolean.prototype);
console.log('defaults', new String().valueOf(), new String(undefined).valueOf(), new Boolean().valueOf());
class Label extends String {
    tag = 'label';
    #code = 7;
    target: unknown;
    constructor(value: unknown) {super(value); this.target = new.target;}
    describe(): string {return super.valueOf() + ':' + this.tag + ':' + this.#code;}
}
class Flag extends Boolean {tag = 9; read(): boolean {return super.valueOf();}}
const label = new Label('name'), derivedFlag = new Flag(false);
console.log('derived', label instanceof Label, label instanceof String, label.target === Label, label.describe(), derivedFlag instanceof Flag, derivedFlag instanceof Boolean, derivedFlag.read(), derivedFlag.tag);
class Deep extends Label {}
console.log('deep', new Deep('deep').target === Deep, new Deep('deep').describe());
for (const method of [String.prototype.valueOf, String.prototype.toString, Boolean.prototype.valueOf, Boolean.prototype.toString]) {
    try {(method as any).call({});} catch(error) {console.log('brand', error instanceof TypeError);}
}
const old = String.prototype.toString;
String.prototype.toString = function(): string {return 'custom';};
console.log('override', text.toString(), String(text));
String.prototype.toString = old;
(String.prototype as any).extra = 5;
let keys = '';
for (const key in 'x' as any) keys += key + ',';
console.log('for-in', keys);
console.log('metadata', String.name, String.length, Boolean.name, Boolean.length);
try {class Invalid extends String {length = 3;} new Invalid('a');} catch(error) {console.log('non-configurable', error instanceof TypeError);}
const detached: any = new String('detached');
Object.setPrototypeOf(detached, null);
console.log('detached', Object.getPrototypeOf(detached), detached.valueOf, detached.toString, detached[0]);
function shadow(): number {
    const String = (value: number) => value + 1;
    const Boolean = (value: number) => value + 2;
    return String(1) + Boolean(1);
}
console.log('shadow', shadow());

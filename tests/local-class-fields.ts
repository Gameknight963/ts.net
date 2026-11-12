export {};
class Small {value = 1; label = 'a'; enabled = true;}
function kernel(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const value = new Small(); const alias = value;
        alias.value = i + 1; alias.enabled = (i & 1) === 0;
        if (alias.enabled) total += value.value;
        total += value.label.length;
    }
    return total;
}
console.log('kernel', kernel(10));
function initial(): string {const value = new Small(); return value.value + ':' + value.label + ':' + value.enabled;}
console.log('initial', initial());
function escape(): Small {const value = new Small(); return value;}
console.log('escape', escape() instanceof Small, escape() !== escape());
let calls = 0;
class Effects {value = ++calls;}
function effects(): number {const value = new Effects(); return value.value;}
console.log('effects', effects(), effects(), calls);
class Explicit {value = 0; constructor() {calls += 10; this.value = calls;}}
function explicit(): number {const value = new Explicit(); return value.value;}
console.log('constructor', explicit(), calls);
class Derived extends Small {extra = 3;}
function derived(): number {const value = new Derived(); return value.value + value.extra;}
console.log('derived', derived());
class Method {value = 1; get read(): number {calls++; return this.value;}}
function method(): number {const value = new Method(); return value.read;}
console.log('method', method(), calls);
let Selected = class {value = 5;};
function selected(): number {const value = new Selected(); return value.value;}
Selected = class {value = 7;};
console.log('rebound', selected());
class Plain {value = 1;}
Object.defineProperty(Plain.prototype, 'value', {get() {return 11;}, configurable: true});
function own(): number {const value = new Plain(); return value.value;}
console.log('own before prototype', own());
function deleted(): number {const value: any = new Plain(); delete value.value; return value.value;}
console.log('deleted', deleted());
class Defaults {value!: number;}
function defaults(): void {const value = new Defaults(); console.log('default', value.value, Object.hasOwn(value, 'value'));}
defaults();
function defaultRead(): number {const value = new Defaults(); return value.value;}
console.log('default local', defaultRead());
function before(count: number): number {let total = 0;for (let i = 0; i < count; i++) {const value = new Later(); total += value.value;}return total;}
console.log('tdz empty', before(0));
try {before(1);} catch (error) {console.log('tdz', (error as Error).name);}
class Later {value = 2;}
console.log('ready', before(2));

export let Exported = class {value = 13;};
function exported(): number {const value = new Exported(); return value.value;}
Exported = class {value = 17;};
console.log('export rebound', exported());
namespace Namespace {export let Choice = class {value = 19;}; export function read(): number {const value = new Choice(); return value.value;}}
Namespace.Choice = class {value = 23;};
console.log('namespace rebound', Namespace.read());
const Anonymous = class {value = 29;};
function anonymous(): number {const value = new Anonymous(); return value.value;}
console.log('anonymous', anonymous());

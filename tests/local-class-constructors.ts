export {};
class Holder {value = 0; constructor(value: number) {this.value = value;}}
function kernel(count: number): number {let total = 0;for (let i = 0; i < count; i++) {total += new Holder(i).value;}return total;}
function escape(): Holder {const value = new Holder(7); return value;}
console.log('kernel', kernel(10), 'escape', escape().value, escape() instanceof Holder);
const trace: string[] = [];
function argument(name: string, value: number): number {trace.push(name); return value;}
class Pair {first = 0; second = 0; constructor(first: number, second: number) {this.second = second; this.first = first;}}
function order(): string {const value = new Pair(argument('first', 3), argument('second', 5)); return value.first + ':' + value.second;}
console.log('arguments', order(), trace.join(','));
class Mixed {text = ''; enabled = false; constructor(text: string, enabled: boolean) {this.text = text; this.enabled = enabled;}}
function mixed(): string {const value = new Mixed('hello', true); return value.text + ':' + value.enabled;}
console.log('mixed', mixed());
class Empty {value = 11; constructor() {}}
function empty(): number {const value = new Empty(); return value.value;}
console.log('empty', empty());
let effects = 0;
class Effect {value = 0; constructor(value: number) {effects++; this.value = value;}}
function effect(): number {const value = new Effect(13); return value.value;}
console.log('effect', effect(), effects);
class Replaced {value = 0; constructor(value: number) {return {value: value + 1} as any;}}
function replaced(): number {const value = new Replaced(17); return value.value;}
console.log('replaced', replaced());
class Default {value = 0; constructor(value = 19) {this.value = value;}}
function defaults(): number {const value = new Default(); return value.value;}
console.log('default', defaults());
const Factory = (() => Holder)();
function factory(): number {const value = new Factory(23); return value.value;}
console.log('factory', factory());
let Selected = Holder;
Selected = class {value = 0; constructor(value: number) {this.value = value + 1;}};
function selected(): number {const value = new Selected(29); return value.value;}
console.log('selected', selected());
Object.defineProperty(Holder.prototype, 'value', {get() {throw new Error('own field must win');}, configurable: true});
console.log('own', kernel(3));
function before(): number {const value = new Later(31); return value.value;}
try {before();} catch (error) {console.log('tdz', (error as Error).name);}
class Later {value = 0; constructor(value: number) {this.value = value;}}
console.log('ready', before());

const EmptyFactory = (() => Empty)();
function emptyFactory(): number {const value = new EmptyFactory(); return value.value;}
console.log('empty factory', emptyFactory());
function localArgument(count: number): number {let total = 0;for (let i=0;i<count;i++) {const value = new Holder(i);total += value.value;}return total;}
console.log('named local', localArgument(10));
function nested(): number {return new Holder(new Holder(37).value).value;}
console.log('nested', nested());

class Declared {value: number; constructor(value: number) {this.value = value;}}
console.log('declared', new Declared(41).value);
const unknown: any = 43;
console.log('unknown argument', new Holder(unknown).value);
class Rest {value = 0; constructor(...values: number[]) {this.value = values[0];}}
console.log('rest', new Rest(47).value);
class FieldEffect {value = ++effects; constructor(value: number) {this.value = value;}}
console.log('field effect', new FieldEffect(53).value, effects);
function argumentBefore(): number {return new ArgumentLater(argument('late argument', 59)).value;}
const beforeTrace = trace.length;
try {argumentBefore();} catch {console.log('argument tdz', trace.length === beforeTrace);}
class ArgumentLater {value = 0; constructor(value: number) {this.value = value;}}
console.log('argument ready', argumentBefore(), trace[trace.length-1]);

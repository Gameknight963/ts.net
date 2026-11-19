export {};
const events: string[] = [];
function initial(): number {events.push('field'); return 1;}
function argument(value: number): number {events.push(`argument:${value}`); return value;}
class Item {
    static initialized = events.push('static');
    value = initial();
    constructor(first: number, second: number) {events.push('constructor'); this.value += first + second;}
    read(): number {return this.value;}
}
const first = new Item(argument(2), argument(3));
const second = new Item(argument(4), argument(5));
console.log('order', events.join(','), first.read(), second.read(),
    Object.getPrototypeOf(first) === Item.prototype, first instanceof Item);
events.length = 0;
let published: any;
function publish(value: any): number {published = value; events.push(`keys:${Object.keys(value).join(',')}`); return 2;}
class Escaped {
    first = publish(this);
    second = initial();
    constructor(value: number) {events.push(`body:${Object.keys(this).join(',')}`); this.first += value;}
}
const escaped = new Escaped(7);
console.log('escaped', escaped === published, escaped.first, escaped.second, events.join(','));
function freeze(value: any): number {published = value; Object.freeze(value); return 3;}
class Frozen {
    value = freeze(this);
    constructor(value: number) {events.push('unexpected body'); this.value += value;}
}
try {new Frozen(1);} catch (error) {console.log('frozen', (error as Error).name, Object.keys(published).length);}
class Replaced {
    value = initial();
    constructor(value: number) {events.push('replace'); return {value: value + 10};}
}
const replaced = new Replaced(8);
console.log('replacement', replaced.value, replaced instanceof Replaced);
class Primitive {
    value = initial();
    constructor(value: number) {events.push(`primitive:${value}`); return 3 as any;}
}
console.log('primitive', new Primitive(9).value);
class EmptyArguments {
    value = initial();
    constructor() {events.push('zero constructor');}
}
console.log('zero', new EmptyArguments().value);
function capture(offset: number): number {
    class Captured {value = offset; constructor(value: number) {this.value += value;}}
    return new Captured(4).value;
}
console.log('captured fallback', capture(1), capture(2));
function make(label: string) {
    class Named {value = initial(); read(): string {return label + ':' + this.value;}}
    return new Named();
}
const namedA = make('a'), namedB = make('b');
console.log('distinct owners', namedA.read(), namedB.read(), Object.getPrototypeOf(namedA) !== Object.getPrototypeOf(namedB));
class Defaults {value: number; constructor(value = initial()) {this.value = value;}}
console.log('default fallback', new Defaults().value, new Defaults(12).value);
class Observed {value = 0; constructor(value: number) {this.value = value + arguments.length;}}
console.log('arguments fallback', new Observed(13).value);
class ReturnedArguments {value = 0; constructor(value: number) {return {value: value + arguments.length};}}
console.log('returned arguments fallback', new ReturnedArguments(14).value);
class Parameterized {constructor(public value: number) {events.push('parameter property');}}
console.log('parameter property', new Parameterized(16).value);
function target(value: any): number {events.push(`target:${value.name}`); return 14;}
class Target {value = 0; constructor() {this.value = target(new.target);}}
console.log('target fallback', new Target().value);
class Derived extends Item {extra = initial(); constructor(value: number) {super(value, 1);}}
console.log('derived fallback', new Derived(15).read());
function early(): Late {return new Late(argument(99));}
try {early();} catch (error) {console.log('early binding', (error as Error).name);}
class Late {value = 0; constructor(value: number) {this.value = initial() + value;}}
console.log('late binding', new Late(1).value);
console.log('effects', events.join(','));

class MixedArguments {
    value = initial();
    label = '';
    enabled = false;
    constructor(value: number, label: string, enabled: boolean) {
        events.push(`mixed:${label}:${enabled}`);
        this.value += value; this.label = label; this.enabled = enabled;
    }
}
function textArgument(): string {events.push('text argument'); return 'item';}
function booleanArgument(): boolean {events.push('boolean argument'); return true;}
events.length = 0;
const mixed = new MixedArguments(argument(4), textArgument(), booleanArgument());
console.log('mixed arguments', mixed.value, mixed.label, mixed.enabled, events.join(','), mixed instanceof MixedArguments);
class ReferenceArguments {
    value: any;
    constructor(value: {id: number}, values: number[], callback: (value: number) => number) {
        this.value = [value, values, callback(value.id)];
    }
}
const record = {id: 7}, values = [2, 3];
const references = new ReferenceArguments(record, values, value => value + 1);
console.log('reference arguments', references.value[0] === record, references.value[1] === values, references.value[2]);
class ManyArguments {
    value: number;
    constructor(a: number, b: number, c: number, d: number) {this.value = a + b + c + d;}
}
console.log('four arguments', new ManyArguments(1, 2, 3, 4).value);
class MixedReplacement {
    value = 0;
    constructor(label: string, enabled: boolean, value: number) {return {value: enabled ? value : label.length};}
}
console.log('mixed replacement', new MixedReplacement('word', false, 9).value, new MixedReplacement('word', true, 9) instanceof MixedReplacement);
class MixedObserved {
    value = '';
    constructor(label: string, enabled: boolean, value: number) {this.value = `${label}:${enabled}:${value}:${arguments.length}`;}
}
console.log('mixed arguments fallback', new MixedObserved('item', true, 3).value);
class MixedDefault {
    value: string;
    constructor(label: string, enabled = true) {this.value = `${label}:${enabled}`;}
}
console.log('mixed default fallback', new MixedDefault('item').value, new MixedDefault('item', false).value);

function inspectLiteralFields(value: any): number {
 const descriptor=Object.getOwnPropertyDescriptor(value,'value')!;
 console.log('literal fields before body',Object.keys(value).join(','),value.value,value.label,value.enabled,descriptor.writable,descriptor.enumerable,descriptor.configurable);
 return 7;
}
class LiteralFields {
 value=0; label='ready'; enabled=false;
 constructor(){this.value=inspectLiteralFields(this);}
}
const literalFields=new LiteralFields();
console.log('literal fields after body',literalFields.value,literalFields.label,literalFields.enabled,literalFields instanceof LiteralFields);
function inspectUndefinedFields(value: any): number {
 console.log('literal undefined',Object.keys(value).join(','),value.value===undefined,value.label===undefined,value.enabled===undefined);
 return 1;
}
class UndefinedFields {
 value: number; label: string; enabled: boolean;
 constructor(){inspectUndefinedFields(this);this.value=3;this.label='set';this.enabled=true;}
}
const undefinedFields=new UndefinedFields();
console.log('undefined fields assigned',undefinedFields.value,undefinedFields.label,undefinedFields.enabled);
let literalVoidEffects=0;
class EffectfulUndefinedFields {
 value=void(literalVoidEffects++);
 constructor(){inspectUndefinedFields(this);}
}
new EffectfulUndefinedFields();
console.log('void initializer effects',literalVoidEffects);

function inspectWideFields(value: any): number {console.log('wide literal fields',Object.keys(value).join(','),value.f0,value.f15,value.f16===undefined,value.f31);return 42;}
class WideLiteralFields {
 f0=0;
 f1=1;
 f2=2;
 f3=3;
 f4=4;
 f5=5;
 f6=6;
 f7=7;
 f8=8;
 f9=9;
 f10=10;
 f11=11;
 f12=12;
 f13=13;
 f14=14;
 f15=15;
 f16!: number;
 f17=17;
 f18=18;
 f19=19;
 f20=20;
 f21=21;
 f22=22;
 f23=23;
 f24=24;
 f25=25;
 f26=26;
 f27=27;
 f28=28;
 f29=29;
 f30=30;
 f31=31;
 constructor(){this.f0=inspectWideFields(this);}
}
const wideLiteralFields=new WideLiteralFields();
console.log('wide literal result',wideLiteralFields.f0,wideLiteralFields.f15,wideLiteralFields.f16===undefined,wideLiteralFields.f31);

export {};
class Mixed {constructor(public value: number, public label = 'default', public enabled = true) {}}
class Holder {constructor(public value = 7) {}}
function kernel(count: number): number {let total = 0;for (let i=0;i<count;i++) {const mixed = new Mixed(11);total += new Holder().value + (mixed.enabled && mixed.label === 'default' ? mixed.value : 0);}return total;}
function escape(): Holder {return new Holder();}
console.log('kernel', kernel(10), escape().value, escape() instanceof Holder);
function mixed(): string {const value = new Mixed(11);return value.value + ':' + value.label + ':' + value.enabled;}
console.log('mixed', mixed());
function missing(): number {return undefined as unknown as number;}
console.log('supplied', new Holder(13).value, new Holder(missing()).value, new Holder(undefined).value);
let effects = 0;
class Effect {constructor(public value = ++effects) {}}
console.log('effect', new Effect().value, new Effect().value, effects);
class Earlier {constructor(public first = 17, public second = first + 1) {}}
console.log('earlier', new Earlier().first, new Earlier().second);
class Fields {value = 0; constructor(value = 19) {this.value = value;}}
function fields(): number {const value = new Fields();return value.value;}
console.log('fields', fields());

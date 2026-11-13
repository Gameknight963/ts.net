export {};
class Counter {
    base = 2;
    read(value: number): number {return this.base + value;}
}
function invoke(target: {read(value: number): number}, value: number): number {return target.read(value);}
const counter = new Counter();
const original = counter.read;
console.log('initial', counter.read(3), invoke(counter, 4));
const events: string[] = [];
Object.defineProperty(counter, 'read', {get() {events.push('get'); return original;}, configurable: true});
function argument(): number {
    events.push('arg'); counter.base = 10;
    Object.defineProperty(counter, 'read', {value: (value: number) => value + 70, writable: true, configurable: true});
    return 3;
}
console.log('lookup before argument', counter.read(argument()), events.join(','));
console.log('replacement after lookup', invoke(counter, 1));
delete (counter as any).read;
console.log('remove shadow', invoke(counter, 1));
Object.defineProperty(counter, 'read', {get() {events.push('get'); return original;}, configurable: true});
function fail(): number {events.push('throw'); throw new Error('argument error');}
try {counter.read(fail());} catch (error) {console.log((error as Error).message, events.join(','));}
delete (counter as any).read;
class Child extends Counter {read(value: number): number {return this.base * value;}}
console.log('derived', invoke(new Child(), 4));
const structural = {base: 30, read: original};
console.log('borrowed', invoke(structural, 7));
console.log('bound', invoke({read: original.bind({base: 40})}, 2));
class Echo {read(value: number): number {return value;}}
const echo = new Echo();
const missing: number = undefined as any;
console.log('return undefined', echo.read(missing), Number.isNaN(echo.read(missing) + 1));
class FieldOnly {base = 3; read(value: number): number {return this.base;}}
const field = new FieldOnly(); field.base = missing;
console.log('undefined field', field.read(1));
class Defaults {base = 5; read(value = 3): number {return this.base + value;}}
console.log('default argument', new Defaults().read(missing));

let reads = 0;
Object.defineProperty(counter, 'base', {get() {return ++reads;}, configurable: true});
console.log('field getter', invoke(counter, 5), reads);
Object.defineProperty(counter, 'base', {get() {throw new Error('field error');}, configurable: true});
try {invoke(counter, 5);} catch (error) {console.log((error as Error).message);}

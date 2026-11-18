const events: string[] = [];
class Ordered {
 a = ((this as any).b = 1);
 b = 2;
 text = "name";
 flag = true;
 absent!: string;
}
const ordered = new Ordered();
console.log(Object.keys(ordered).join(","), ordered.a, ordered.b, ordered.text, ordered.flag, ordered.absent === undefined);
console.log(JSON.stringify(Object.getOwnPropertyDescriptor(ordered, "text")));
class FrozenLater {
 first = (Object.defineProperty(this, "later", { value: 1, writable: false, configurable: false }), 3);
 later = 2;
}
try { new FrozenLater(); } catch { console.log("fixed later field rejected"); }
Object.defineProperty(Object.prototype, "shadow", { configurable: true, set(value: string) { events.push(value); } });
class Shadow { shadow = "own"; }
const shadow = new Shadow();
console.log(shadow.shadow, Object.hasOwn(shadow, "shadow"), events.length);
delete (Object.prototype as any).shadow;
class Base { value = 1; text = "base"; static value = 7; }
class Derived extends Base { next = this.value + 2; }
const derived = new Derived();
console.log(derived.value, derived.text, derived.next, Base.value, Object.keys(derived).join(","));
class Captured {
 value = 2;
 read = () => this.value;
 name = "captured";
}
const captured = new Captured(); captured.value = 4;
console.log(captured.read(), captured.read.name, captured.name);
class FailAfterValue {
 value = 3;
 next = (() => { throw new Error("initializer"); })();
}
try { new FailAfterValue(); } catch (error) { console.log((error as Error).message); }

function missingString(): string { return undefined as any; }
function missingBoolean(): boolean { return undefined as any; }
class Missing { text = missingString(); enabled = missingBoolean(); }
const missing = new Missing();
console.log(missing.text === undefined, missing.enabled === undefined, Object.keys(missing).join(","));
Object.defineProperty(Object.prototype, "locked", { value: 9, writable: false, configurable: true });
class OwnLocked { locked = 1; }
console.log(new OwnLocked().locked);
delete (Object.prototype as any).locked;
class UnknownFields { value: unknown = 4; text: unknown = "text"; enabled: unknown = true; }
const unknownFields = new UnknownFields();
console.log(unknownFields.value, unknownFields.text, unknownFields.enabled, Object.keys(unknownFields).join(","));
function numericInitializer(value: number): number {events.push('number'); return value;}
class NumericValues {
 missing = numericInitializer(undefined as any);
 nan = numericInitializer(NaN);
 negativeZero = numericInitializer(-0);
 infinity = numericInitializer(Infinity);
}
const numbers = new NumericValues();
console.log('numeric values', numbers.missing === undefined, Object.hasOwn(numbers, 'missing'),
 Number.isNaN(numbers.nan), Object.is(numbers.negativeZero, -0), numbers.infinity);
console.log('numeric descriptors', Object.keys(numbers).join(','), Object.getOwnPropertyDescriptor(numbers, 'missing')!.value === undefined);
console.log('materialized numeric values', numbers.missing === undefined, Number.isNaN(numbers.nan), Object.is(numbers.negativeZero, -0));

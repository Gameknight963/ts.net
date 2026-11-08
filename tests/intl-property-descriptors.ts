/// <reference lib="es2022.intl" />
export {};
const number = new Intl.NumberFormat('en-US', {useGrouping: false});
Object.defineProperty(number, 'label', {value: 'number', enumerable: true});
const format = number.format;
Object.freeze(number);
console.log('number', number.format(12.5), number.format === format, Object.isFrozen(number), Object.keys(number).join(','), Reflect.set(number, 'label', 'other'));
const collator = Object.freeze(new Intl.Collator('en-US'));
console.log('collator', collator.compare('a', 'b') < 0, collator.compare === collator.compare, Object.isFrozen(collator));
const date = Object.freeze(new Intl.DateTimeFormat('en-US', {timeZone: 'UTC', year: 'numeric'}));
console.log('date', date.format(0), Object.isFrozen(date));
const locale = Object.freeze(new Intl.Locale('en-US'));
console.log('locale', locale.language, locale.region, locale.toString(), Object.isFrozen(locale));
const segmenter = Object.freeze(new Intl.Segmenter('en-US'));
const segments = Object.freeze(segmenter.segment('a??b'));
const iterator = Object.freeze(segments[Symbol.iterator]());
console.log('segments', iterator.next().value!.segment, iterator.next().value!.segment, iterator.next().value!.segment, iterator.next().done, Object.isFrozen(segments));
for (const prototype of [Intl.NumberFormat.prototype, Intl.Collator.prototype, Intl.DateTimeFormat.prototype, Intl.Locale.prototype, Intl.Segmenter.prototype, Intl, Object.getPrototypeOf(iterator)]) {
    const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
}

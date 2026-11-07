class Dictionary {[key: string]: number | undefined; initial = 1;}
const dictionary = new Dictionary(); dictionary.next = 2;
console.log('dictionary', dictionary.initial, dictionary['next'], dictionary.missing, Object.keys(dictionary).join(','));
class Numeric {[index: number]: string;}
const numeric = new Numeric(); numeric[3] = 'three'; numeric[-1] = 'negative';
console.log('numeric', numeric[3], numeric[-1], numeric[7], Object.keys(numeric).join(','));
class Lookup<T> {[key: string]: T;}
const numbers = new Lookup<number>(); numbers.answer = 42;
console.log('generic', numbers.answer, Object.keys(numbers).length);
class Derived extends Dictionary {other = 3;}
const derived = new Derived(); derived.dynamic = 4;
console.log('derived', derived.initial, derived.other, derived.dynamic, Object.keys(derived).join(','));
class StaticEntries {static [key: string]: any; static initial = 5;}
StaticEntries.next = 6;
console.log('static', StaticEntries.initial, StaticEntries.next, Object.keys(StaticEntries).join(','));
console.log('type only', Object.keys(new Numeric()).length, Object.keys(new Lookup<string>()).length);

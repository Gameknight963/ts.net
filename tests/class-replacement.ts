const replacement = {value: 40, text: 'replacement', read(): number { return this.value; }};
class Base {
    value = 1;
    text = 'allocated';
    constructor() { return replacement as any; }
    read(): number { return -1; }
}
const base = new Base();
console.log('returned', base === replacement, base.value, base.text, base.read(), base instanceof Base);
base.value += 2;
console.log('updated', base.value++, base.value, replacement.value);
console.log('optional', base?.value, base.read?.());
const {value, text} = base;
console.log('destructure', value, text);
for (base.value of [50, 51]) console.log('iteration', base.value);
class Derived extends Base {
    extra = 7;
    #secret = 9;
    saved = () => this.#secret;
    constructor() { super(); this.value++; }
}
const derived = new Derived();
console.log('derived', derived === replacement, derived.value, derived.extra, derived.saved(), derived instanceof Derived);
function read(input: Base): number { return input.value; }
function forward(input: Derived): Derived { return input; }
console.log('parameters', read(base), forward(derived) === derived);
const copy = {...derived};
console.log('spread', copy.value, copy.extra, copy.saved());

{
const replacement = {value: 4};
class Base {constructor() {return replacement as any;}}
class Derived extends Base {value = 7; extra = 9;}
const value = new Derived();
console.log('returned fixed field', value === replacement, value.value, replacement.value, (value as any).value);
console.log('returned field descriptor', Object.getOwnPropertyDescriptor(value, 'value')!.value);
console.log('returned serialized', JSON.stringify(value));
}

{
const first = {left: 1}, second = {right: 2};
const choose = (condition: boolean) => condition ? first : second;
class Base {constructor(flag: boolean) {return choose(flag) as any;}}
class Derived extends Base {left = 'left'; right = 'right';}
for (const flag of [true, false]) {
 const item = new Derived(flag);
 console.log('returned union fields', item.left, item.right, (flag ? first : second) === (item as any));
}
}

{
const original = {payload: 4};
class Base {constructor() {return original as any;}}
class Derived extends Base {payload = 'text';}
const result = new Derived();
console.log('returned typed field', result.payload, (original as any).payload, JSON.stringify(result));
}

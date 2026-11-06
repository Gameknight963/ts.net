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

export {};
let remove: typeof Reflect.deleteProperty;
remove = Reflect.deleteProperty;
const first = {a: 1};console.log('assignment', remove(first,'a'), first.a);

let reflection: typeof Reflect;
reflection = Reflect;
const second = {b: 2};console.log('namespace', reflection.deleteProperty(second,'b'), second.b);

let chained: typeof Reflect.deleteProperty;
chained = remove;
const third = {c: 3};console.log('chain', chained(third,'c'), third.c);

let optional: typeof Reflect.deleteProperty | undefined;
optional ??= Reflect.deleteProperty;
const fourth = {d: 4};console.log('nullish', optional(fourth,'d'), fourth.d);

let logical: typeof Reflect.deleteProperty | undefined;
logical ||= Reflect.deleteProperty;
const fifth = {e: 5};console.log('or', logical(fifth,'e'), fifth.e);

let assigned: typeof Reflect.deleteProperty;
const sixth = {f: 6};console.log('expression', (assigned = Reflect.deleteProperty)(sixth,'f'), sixth.f);

function replace(target: {g?: number}, operation: typeof Reflect.deleteProperty): boolean {
    operation = Reflect.deleteProperty;return operation(target,'g');
}
const seventh = {g: 7};console.log('parameter assignment', replace(seventh,() => false), seventh.g);

let overwritten: typeof Reflect.deleteProperty = Reflect.deleteProperty;
overwritten = () => false;
const retained = {h: 8};console.log('override', overwritten(retained,'h'), retained.h);

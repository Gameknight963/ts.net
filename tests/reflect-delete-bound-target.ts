export {};

const item = {x: 1, y: 2};
const remove = Reflect.deleteProperty.bind(Reflect, item);
console.log('bound target', remove('x'), item.x, Object.keys(item).join(','));
item.x = 3;
console.log('restore', item.x, Object.keys(item).join(','));
console.log('missing', remove('absent'), remove('x'), remove('x'));

const complete = {a: 4, b: 5};
const removeA = Reflect.deleteProperty.bind(null, complete, 'a');
console.log('bound key', removeA(), complete.a, Object.keys(complete).join(','));

const alias = Reflect.deleteProperty;
const aliased = {a: 6, b: 7};
const aliasRemove = alias.bind(undefined, aliased);
console.log('alias', aliasRemove('b'), aliased.b, Object.keys(aliased).join(','));

const {deleteProperty: destructured} = Reflect;
const destructuredTarget = {a: 8};
console.log('destructured', destructured.bind(null, destructuredTarget)('a'), destructuredTarget.a);

const dynamic: any = {a: 9, b: 10};
const dynamicRemove = Reflect.deleteProperty.bind(null, dynamic);
console.log('dynamic', dynamicRemove('b'), dynamic.b, Object.keys(dynamic).join(','));

function bindTarget(target: object): (key: PropertyKey) => boolean {
    return Reflect.deleteProperty.bind(null, target);
}
const hidden = {a: 11};
console.log('parameter', bindTarget(hidden)('a'), hidden.a, Object.keys(hidden).length);

const parent = {a: 12};
const child = {a: 13, b: 14};
Object.setPrototypeOf(child, parent);
console.log('inherited', Reflect.deleteProperty.bind(null, child)('a'), child.a, Object.hasOwn(child, 'a'));

let calls = 0;
const getter = {get a(): number {calls++; return 15;}};
console.log('accessor', Reflect.deleteProperty.bind(null, getter)('a'), calls, Object.keys(getter).length);

const locked = Object.defineProperty({a: 16}, 'a', {configurable: false});
console.log('locked', Reflect.deleteProperty.bind(null, locked)('a'), locked.a);

const values = [1, 2, 3];
console.log('array', Reflect.deleteProperty.bind(null, values)(1), values.length, 1 in values);

let keyCalls = 0;
const key = {toString(): string {keyCalls++; return 'a';}};
const keyed = {a: 17};
const keyedRemove = Reflect.deleteProperty.bind(null, keyed, key as any);
console.log('deferred key', keyCalls, keyed.a);
console.log('coercion', keyedRemove(), keyCalls, keyed.a);

const twice = {a: 18};
const first = Reflect.deleteProperty.bind(null, twice);
console.log('rebound', first.bind(null, 'a')(), twice.a);

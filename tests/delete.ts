const record: {a?: number, b?: number} = {a: 1, b: 2};
console.log('delete', delete record.a, record.a, Object.hasOwn(record, 'a'), Object.keys(record).join(','));
record.a = 3; console.log('reinsert', record.a, Object.keys(record).join(','));
console.log('missing', delete record.a, delete record.a);
const alias: any = {x: 4, y: 5}; const same = alias;
console.log('alias', delete same.x, alias.x, Object.keys(alias).join(','));
const prototype = {x: 8}; const inherited = {x: 9};
Object.setPrototypeOf(inherited, prototype);
console.log('inherited', delete (inherited as any).x, inherited.x, 'x' in inherited, Object.hasOwn(inherited, 'x'));
const sparse = [1, 2, 3]; console.log('array', delete sparse[1], sparse.length, sparse[1], Object.keys(sparse).join(','));
const key = {toString(): string {console.log('coerce'); return 'y';}};
console.log('computed', delete alias[key as any], Object.keys(alias).length);
let effects = 0;
const accessor = {get x(): number {effects++; throw 'getter';}};
console.log('accessor', delete (accessor as any).x, effects);
const object = Object.create(null, {fixed: {value: 1}, removable: {value: 2, configurable: true}});
console.log('configurable', delete object.removable, object.removable);
try {console.log(delete object.fixed);} catch (error) {console.log('locked', (error as Error).name);}
for (const value of [null, undefined]) {try {console.log(delete (value as any).x);} catch(error) {console.log('nullish', (error as Error).name);}}
console.log('primitive', delete (3 as any).x, delete ('abc' as any).other);
try {console.log(delete ('abc' as any)[0]);} catch(error) {console.log('string index', (error as Error).name);}
class Item {x?: number = 10;}
const instance = new Item(); console.log('class', delete instance.x, instance.x, Object.keys(instance).length);
let optional: any = null;
console.log('optional', delete optional?.x, delete optional?.[effects++], effects);
optional = {x: 11};console.log('present', delete optional?.x, optional.x);
optional = {nested: {x: 12}};console.log('nested', delete optional?.nested.x, optional.nested.x);
async function run(): Promise<void> {
    const value: any = {x: 13};
    console.log('await receiver', delete (await Promise.resolve(value)).x, value.x);
    value.x = 14;console.log('await key', delete value[await Promise.resolve('x')], value.x);
    value.x = 15;console.log('await optional', delete value?.[await Promise.resolve('x')], value.x);
    const absent: any = null;console.log('await skipped', delete absent?.[await Promise.resolve('x')]);
}
run();

interface Point {x: number; y: number;}
function sum(value: Point): number {return value.x + value.y;}
function identity<T extends Point>(value: T): T {return value;}
const parsed: Point = JSON.parse('{"x":3,"y":4}');
console.log('json', sum(parsed), identity(parsed) === parsed);
parsed.x += 2;
console.log('writes', parsed.x, parsed.y++, sum(parsed));
const {x, y} = parsed;
console.log('destructure', x, y, JSON.stringify({...parsed}));
for (parsed.x of [11, 13]) console.log('iteration', parsed.x);
console.log('optional chain', parsed?.x, (null as Point | null)?.x);
const dictionary: any = Object.create(null);
dictionary.x = 17; dictionary.y = 19;
console.log('dictionary', sum(dictionary));
let reads = 0, assigned = 23;
const accessor: any = Object.create(null);
Object.defineProperty(accessor, 'x', {get() {reads++; return assigned;}, set(value: number) {console.log('setter receiver', this === accessor); assigned = value;}, enumerable: true});
Object.defineProperty(accessor, 'y', {get() {reads++; return 29;}, enumerable: true});
const checked: Point = accessor;
console.log('check does not read', reads, checked === accessor);
console.log('getter', sum(checked), reads);
checked.x = 31;
console.log('setter', assigned, checked.x, reads);
const inherited: any = Object.create({get x(): number {console.log('inherited receiver', this === inherited); return 37;}, y: 41});
const inheritedPoint: Point = inherited;
console.log('inherited', sum(inheritedPoint));
inheritedPoint.y = 43;
console.log('own write', inheritedPoint.y, Object.hasOwn(inherited, 'y'));
const list: Point[] = JSON.parse('[{"x":2,"y":3},{"x":5,"y":7}]');
console.log('array', list.map(sum).join(','));
interface Box<T> {item: T;}
const box: Box<Point> = JSON.parse('{"item":{"x":47,"y":53}}');
console.log('nested', sum(box.item));
interface Optional {value?: number;}
const optional: Optional = JSON.parse('{}');
console.log('missing optional', optional.value === undefined);
optional.value = 59;
console.log('optional write', optional.value, Object.hasOwn(optional, 'value'));
const methods = {base: 0, read(value: number): number {return this.base + value;}};
interface Reader {read(value: number): number;}
function invoke(value: Reader): number {return value.read(61);}
dictionary.base = 67; dictionary.read = methods.read;
console.log('method', invoke(dictionary));
function lengthView(value: unknown): {length: number} {return value as {length: number};}
console.log('primitive', lengthView('hello').length);
Object.defineProperty(parsed, 'y', {value: 71, writable: false});
try {parsed.y = 73;} catch {console.log('readonly rejected');}
console.log('readonly', parsed.y);

interface Mixed {label: string; flag: boolean; point?: Point;}
const mixed: Mixed = JSON.parse('{"label":"ready","flag":true}');
mixed.flag &&= false;
mixed.label += '!';
mixed.point = parsed;
console.log('mixed fields', mixed.label, mixed.flag, mixed.point === parsed, sum(mixed.point));

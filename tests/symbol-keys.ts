const first = Symbol("key"), second = Symbol("key");
const value: any = {};
value[first] = 10;
value.text = 20;
value[second] = 30;
value[2] = 40;
console.log("read", value[first], value[second], value[Symbol("key")]);
console.log("keys", Object.keys(value).join(","), Object.keys(value).length);
console.log("names", Object.getOwnPropertyNames(value).join(","));
console.log("symbols", Object.getOwnPropertySymbols(value).length, Object.getOwnPropertySymbols(value)[0] === first);
console.log("reflect", Reflect.ownKeys(value).map(key => typeof key === "symbol" ? String(key) : key).join(","));
console.log("values", Object.values(value).join(","), Object.entries(value).map(pair => pair.join(":")).join(","));
let names = ""; for (const key in value) names += key + ",";
console.log("for in", names, JSON.stringify(value));
for (const copy of [Object.assign({}, value), {...value}, Object.fromEntries([[first, 10], [second, 30]])]) {
    console.log("copy", (copy as any)[first], (copy as any)[second], Object.getOwnPropertySymbols(copy).length);
}
const {[first]: removed, ...rest} = value;
console.log("rest", removed, rest[first], rest[second], Object.getOwnPropertySymbols(rest).length);
console.log("descriptor", Object.getOwnPropertyDescriptor(value, first)!.value, Reflect.has(value, first), Reflect.get(value, second));
console.log("delete", Reflect.deleteProperty(value, first), value[first], Object.getOwnPropertySymbols(value).length);
const array: any = [1, 2]; array[first] = 3;
console.log("array", array[first], Object.keys(array).join(","), JSON.stringify(array), Object.getOwnPropertySymbols(array).length);
let reads = 0;
const accessor: any = { get [first]() { reads++; return 99; }, visible: 1 };
console.log("skip getter", Object.values(accessor).join(","), JSON.stringify(accessor), reads);
console.log("copy getter", ({...accessor} as any)[first], reads);
const hidden = Object.create(null, { [first]: {value: 7, enumerable: false}, [second]: {value: 8, enumerable: true} });
console.log("hidden", Object.getOwnPropertySymbols(hidden).length, Object.keys(hidden).length, (Object.assign({}, hidden) as any)[second]);
const descriptors = Object.getOwnPropertyDescriptors(value);
console.log("descriptors", Object.getOwnPropertySymbols(descriptors)[0] === second, descriptors[second as any].value);
const many: any = {};
for (let i = 0; i < 8; i++) many["k" + i] = i;
many[first] = 1; many[second] = 2;
console.log("count", Object.keys(many).length);
delete many.k0; delete many[first];
console.log("count deleted", Object.keys(many).length, Object.getOwnPropertySymbols(many).length);
const clone = {...many};
console.log("count cloned", Object.keys(clone).length, Object.getOwnPropertySymbols(clone).length);
class Keyed { [first] = 42; [second]() { return this[first]; } }
const keyed: any = new Keyed();
console.log("class", keyed[first], keyed[second](), Object.getOwnPropertySymbols(keyed).length);

{
    const hidden = Symbol('keys');
    const source: any = {alpha: 1, beta: 2};
    source[hidden] = 3;
    const first = Object.keys(source);
    first[0] = 'changed'; first.push('extra');
    console.log('independent filtered keys', Object.keys(source).join(','), Reflect.ownKeys(source).map(key => typeof key).join(','));
    Object.defineProperty(source, 'alpha', {value: 4, enumerable: false, configurable: true});
    console.log('mixed key descriptors', Object.keys(source).join(','));
    Object.defineProperty(source, 'alpha', {value: 5, enumerable: true, configurable: true});
    console.log('restored key descriptors', Object.keys(source).join(','));
    delete source[hidden];
    console.log('removed symbol', Object.keys(source).join(','));
    const onlySymbols = {[Symbol('first')]: 1, [Symbol('second')]: 2};
    const empty = Object.keys(onlySymbols); empty.push('local');
    console.log('empty filtered keys', Object.keys(onlySymbols).length, Reflect.ownKeys(onlySymbols).length);
    source['\0symbol:ordinary'] = 6;
    console.log('ordinary nul key', Object.keys(source).includes('\0symbol:ordinary'));
}

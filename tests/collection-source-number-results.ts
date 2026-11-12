export {};
const map = new Map<number, number>([[1, 2], [3, 4]]);
let mapTotal = 0;
function mapValue(value: number, key: number, collection: Map<number, number>): number {
    if (collection !== map) throw new Error('wrong map');
    mapTotal += value + key + collection.size;
    return mapTotal;
}
map.forEach(mapValue);
const set = new Set<number>([1, 2]);
let setTotal = 0;
function setValue(value: number, key: number, collection: Set<number>): number {
    if (collection !== set) throw new Error('wrong set');
    setTotal += value + key + collection.size;
    return setTotal;
}
set.forEach(setValue);
console.log('numeric collection results', mapTotal, setTotal);

let observed = 0;
function observeCall(this: {tag: number}, value: number, key: number, source: Map<number, number>): number {
    observed += arguments.length + this.tag;
    if (source !== map) throw new Error('wrong observed map');
    return value + key;
}
map.forEach(observeCall, {tag: 2});
console.log('observed collection callback', observed);

const changing = new Map<number, number>([[1, 10], [2, 20]]);
let visited = '';
function changeCollection(value: number, key: number, source: Map<number, number>): number | undefined {
    visited += key + ':' + value + ';';
    if (key === 1) {
        source.delete(2);
        source.set(3, 30);
        return undefined;
    }
    return NaN;
}
changing.forEach(changeCollection);
console.log('changed collection callback', visited);

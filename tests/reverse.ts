function show(items: any[]) {
    console.log(items.length);
    for (let i = 0; i < items.length; i++) console.log(items[i], Object.hasOwn(items, i));
}
const empty: any[] = [];
console.log(empty.reverse() === empty);
show(empty);
const single = [9];
console.log(single.reverse() === single, single[0]);
const numbers = [1, 2, 3, 4];
const same = numbers.reverse();
console.log(same === numbers, numbers[0], numbers[3]);
numbers.reverse(...[] as []);
console.log(numbers[0], numbers[3]);
const holes = [1, , undefined, -0, 0 / 0] as number[];
holes.reverse();
show(holes);
console.log(1 / holes[1], holes.includes(undefined as unknown as number), holes.indexOf(undefined as unknown as number));
holes.reverse();
show(holes);
const mixed: any[] = [1, , undefined, null, true, "end"];
mixed.reverse();
show(mixed);
const first = { x: 1 };
const last = { x: 2 };
const objects = [first, last];
objects.reverse()[0].x = 8;
console.log(last.x, first.x, objects[0] === last);
const strings = ["one", "two", "three"];
strings.reverse();
console.log(strings[0], strings[1], strings[2]);
const sparse: any[] = [];
sparse[0] = "front";
sparse[2] = undefined;
sparse[10000] = "back";
sparse.reverse();
console.log(sparse.length, sparse[0], sparse[10000], Object.hasOwn(sparse, 2), Object.hasOwn(sparse, 9998));
sparse.reverse();
console.log(sparse[0], sparse[10000], Object.hasOwn(sparse, 2), Object.hasOwn(sparse, 9998));
const sparseNumbers: number[] = [1];
sparseNumbers[10000] = 2;
sparseNumbers.reverse();
console.log(sparseNumbers.length, sparseNumbers[0], sparseNumbers[10000], Object.hasOwn(sparseNumbers, 1));
let current: any = [1, 2];
const original = current;
function replace() { current = [3, 4]; return "ignored"; }
console.log(current.reverse(replace()) === original, original[0], current[0]);
const state = { x: 0 };
function effect(value: number) { state.x = state.x * 10 + value; return value; }
const dynamic: any = [1, 2, 3];
dynamic.label = "named";
console.log(dynamic.reverse(effect(1), effect(2)) === dynamic, dynamic[0], state.x, dynamic.label);
state.x = 0;
console.log(dynamic.reverse(...[effect(1), effect(2), "ignored"]) === dynamic, dynamic[0], state.x);
function inferred() {
    const items: any[] = [1, 2, 3];
    items.reverse();
    items[0] += 4;
    return items[0] + items[2];
}
console.log(inferred());
function reversedAlias() {
    const items: any[] = [1, 2, 3];
    const alias = items.reverse();
    alias[0] = "alias";
    console.log(items[0], alias === items);
}
reversedAlias();

function views() {
    const items: any[] = [1, "two", undefined, "four"];
    items.reverse();
    items[1] = "write";
    const copy = items.slice(1, 4);
    copy[0] = "copy";
    console.log(items[0], items[1], items[3], copy[0], copy[1], copy[2]);
    items.push("tail");
    items.reverse();
    console.log(items.pop(), items[0], items[1], items.length);
    items.reverse();
    items.length = 2;
    items.length = 5;
    console.log(items[0], items[1], Object.hasOwn(items, 2), items[4]);
    items.reverse();
    const spread = [...items];
    console.log(spread[0], spread[3], spread[4], Object.hasOwn(spread, 0));
    items.push(...items);
    console.log(items.length, items[0], items[4], items[5], items[9]);
}
views();
function reverseScan(count: number) {
    const items: any[] = [1, "two", 3, "four"];
    items.reverse();
    let total = 0;
    for (let i = 0; i < count; i++) if (typeof items[i] === "number") total += items[i];
    return total;
}
console.log(reverseScan(0), reverseScan(1), reverseScan(2), reverseScan(3), reverseScan(4));
const searches: any[] = [1, , undefined, "end"];
searches.reverse();
console.log(searches.indexOf(undefined), searches.indexOf("end"), searches.includes(undefined));
show(searches);
const zeros: any[] = [-0, 0 / 0, "end"];
zeros.reverse();
console.log(1 / zeros[2], typeof zeros[1], zeros[1] === undefined);
function reverseGrid() {
    let total = 0;
    for (let length = 0; length < 41; length++) {
        const items: any[] = [];
        items.length = length;
        for (let i = 0; i < length; i++) {
            if (i % 3 === 0) items[i] = i;
            else if (i % 3 === 1) items[i] = undefined;
        }
        items.reverse();
        for (let i = 0; i < length; i++) {
            if (Object.hasOwn(items, i)) total += i;
            if (typeof items[i] === "number") total += items[i];
        }
        items.reverse();
        for (let i = 0; i < length; i++) if (Object.hasOwn(items, i)) total += i;
    }
    return total;
}
console.log(reverseGrid());
const captured: any = [1, 2];
function shadow() { captured.reverse = 3; return 0; }
console.log(captured.reverse(shadow()) === captured, captured[0]);
const spreadCaptured: any = [1, 2];
function shadowSpread() { spreadCaptured.reverse = 3; return 0; }
console.log(spreadCaptured.reverse(...[shadowSpread()]) === spreadCaptured, spreadCaptured[0]);
const spreadSource: any[] = [1, "two"];
const spreadArray: any[] = [...spreadSource, 3];
spreadArray.reverse();
show(spreadArray);
console.log([1, "two"].reverse()[0]);

type X = { x: number };
function scalar(count: number, seed: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        let point = { x: i, y: seed };
        let view: X = point;
        if (i % 2 === 0) view.x += 1;
        total += point.x + point.y;
    }
    return total;
}
console.log(scalar(100, 3));

function escaping(value: number) { const point = { x: value }; return point; }
const escaped = escaping(7);
const escapedAlias = escaped;
escapedAlias.x++;
console.log(escaped.x, escaped === escapedAlias, escaped !== escaping(8));
function read(point: X) { point.x += 2; return point.x; }
function passed() { const point = { x: 1 }; return read(point) + point.x; }
console.log(passed());
function dynamicView() {
    const point = { x: 1 };
    const view: any = point;
    view.x = 5;
    return point.x;
}
console.log(dynamicView());
function identity() { const point = { x: 1 }; const other = { x: 1 }; return point === other; }
console.log(identity());
function mutableAlias() { const point = { x: 1 }; let view = point; view = { x: 9 }; return point.x + view.x; }
console.log(mutableAlias());
function optionalFields(flag: boolean) {
    const point: { x?: number; y?: boolean } = { x: flag ? 3 : undefined, y: flag ? false : undefined };
    const view = point;
    console.log(view.x, view.y);
    view.x = 9;
    view.y = true;
    console.log(point.x, point.y);
}
optionalFields(true);
optionalFields(false);
function missingField() { const point: { x?: number } = {}; point.x = 4; return point.x; }
console.log(missingField());
function stringsAndBooleans(flag: boolean) {
    const point = { text: "a", active: flag };
    const view = point;
    view.text += "b";
    view.active = !view.active;
    return point.active ? point.text : "off";
}
console.log(stringsAndBooleans(true), stringsAndBooleans(false));
function initializer(counter: X) { counter.x++; return counter.x; }
function ordered() {
    const counter = { x: 0 };
    const point = { x: initializer(counter), y: initializer(counter) };
    const alias = point;
    alias.x += point.y;
    return point.x + point.y + counter.x;
}
console.log(ordered());
function nested() {
    let total = 0;
    for (let i = 0; i < 10; i++) {
        const point = { x: i, y: 1 };
        const first: X = point;
        const second = first;
        for (let j = 0; j < 3; j++) {
            if (j === 1) continue;
            second.x++;
        }
        total += point.x + point.y;
    }
    return total;
}
console.log(nested());
function reentrant() {
    const point = { x: 1 };
    const view = point;
    console.log(view.x += (point.x = 9), point.x);
    console.log(view.x++, ++point.x, view.x);
}
reentrant();
function checkedWrite(value: any) { const point = { x: 1 }; point.x = value; return point.x; }
console.log(checkedWrite(undefined));
function nestedEscape() { const point = { x: 1 }; const wrapper = { x: point }; return wrapper.x; }
console.log(nestedEscape().x);
function shorthandEscape() { const x = { x: 2 }; const wrapper = { x }; return wrapper.x; }
console.log(shorthandEscape().x);
function copiedField() { const point = { x: 3 }; const wrapper = { x: point.x }; return wrapper.x; }
console.log(copiedField());

function reassignedRoot() {
    let point = { x: 1 };
    const previous = point;
    point = { x: 9 };
    previous.x++;
    return previous.x + point.x;
}
console.log(reassignedRoot());
function redeclaredRoot() {
    var point = { x: 1 };
    const previous = point;
    var point = { x: 9 };
    previous.x++;
    return previous.x + point.x;
}
console.log(redeclaredRoot());

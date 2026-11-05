function references(count: number) {
    const root = {items: [] as any[], point: {x: 1}, value: undefined as any, unknown: null as unknown};
    const alias = root;
    for (let i = 0; i < count; i++) {
        alias.items = [i, "x", true];
        root.point.x++;
        root.value = i;
        alias.unknown = "done";
    }
    console.log(root.items.length, root.items[0], alias.point.x, root.value, root.unknown);
    const child = root.point;
    child.x = 9;
    console.log(alias.point.x);
}
references(0);references(3);
function child() { const holder = {point: {x: 1}}; return holder.point; }
const escaped = child();escaped.x = 7;
console.log(escaped.x);
function escapeReference() { const root = {values: [1], point: {x: 2}}; return root; }
const whole = escapeReference();whole.values.push(3);whole.point.x++;
console.log(whole.values.length, whole.point.x);
const source = {values: [] as any[]};
const view: {values: any[]} = source;
view.values = [3, "hello"];
console.log(source.values[0], source.values[1]);
const mutable = {value: undefined as any};
mutable.value = mutable;
console.log(mutable.value === mutable);

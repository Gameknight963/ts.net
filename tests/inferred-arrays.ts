function numeric(count: number) {
    const items: any[] = [1, 2, , undefined];
    items[2] = 3;
    let total = 0;
    for (let i = 0; i < count; i++) {
        items[i & 3] = i;
        total += items[i % 4];
    }
    console.log(items[0], items[3], items.length);
    return total;
}
console.log(numeric(9), numeric(0));
function operations() {
    const items: any[] = [1, , undefined, 0 / 0, -0];
    console.log(typeof items[0], typeof items[1], items[1] === undefined, items[2] === null);
    console.log(items.includes(undefined), items.indexOf(undefined), items.includes(0 / 0), items.indexOf(0 / 0));
    console.log(1 / items[4], items.pop(), items.length);
    console.log(items.push(3, undefined), items.pop(), items.pop());
    items[1] ??= 4;
    items[1] += items[0];
    console.log(items[1]++, ++items[1], items[1]);
    items[2] ||= 7;
    items[0] &&= 8;
    console.log(items[0], items[2], items[9] + 1);
    items.length = 1;
    items.length = 4;
    console.log(items[1], items.indexOf(undefined), items.includes(undefined));
}
operations();
function anyBinding() {
    const items: any = [1, 2];
    items[0] += 3;
    console.log(items[0], items.pop(), items.push(9), items.length, items.includes("9"));
}
anyBinding();
function changing() {
    const items: any[] = [1, 2];
    items[0] = "changed";
    console.log(items[0], items[0] + 3);
}
changing();
function alias() {
    const items: any[] = [1, 2];
    const view = items;
    view[0] = "alias";
    console.log(items[0], view === items);
}
alias();
function change(items: any[]) { items[0] = "escaped"; }
function escaping() {
    const items: any[] = [1, 2];
    change(items);
    console.log(items[0]);
}
escaping();
function copied() {
    const items: any[] = [1, 2];
    const copy = items.slice();
    copy[0] = "copy";
    console.log(items[0], copy[0]);
}
copied();
function nullableKey(key: number) {
    const items: any[] = [1, 2];
    items[key] = 9;
    console.log(items[key], items[0 / 0], items.length);
}
nullableKey(undefined as unknown as number);
function coercions() {
    const items: any[] = [1, 2];
    items[0] += "x";
    items[1] ??= "ignored";
    console.log(items[0], items[1]);
}
coercions();
function named() {
    const items: any[] = [1];
    items[-1] = 3;
    items[0 / 0] = 4;
    items[1.5] = 5;
    console.log(items[-1], items[0 / 0], items[1.5], items.length);
}
named();
function changedLength() {
    const items: any[] = [1, 2];
    const dynamic: any = items;
    dynamic.length = "1";
    console.log(items.length, items[1]);
}
changedLength();
function returned() { const items: any[] = [1, 2]; return items; }
const returnedItems = returned();
returnedItems[0] = "returned";
console.log(returnedItems[0]);
function freshAny() {
    const items: any = [1, 2];
    items[0] = "fresh";
    console.log(items[0]);
}
freshAny();
function nonnumeric() {
    const items: any[] = [1, null, true];
    console.log(items[1], items[2], items[1] + 2, items[2] + 2);
}
nonnumeric();

function iterationWrite() {
    const items: any[] = [1];
    for (items[0] of ["iteration"]) {}
    console.log(items[0]);
}
iterationWrite();
function parameterDefault(items: any[] = [1]) { items[0] = "parameter"; return items[0]; }
console.log(parameterDefault());
function fieldAlias() {
    const items: any[] = [1];
    const holder = { length: items };
    holder.length[0] = "field alias";
    console.log(items[0]);
}
fieldAlias();

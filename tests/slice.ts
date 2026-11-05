const values = [1, 2, 3, 4, 5];
function show(items: any[]) { console.log(items.length, items[0], items[1], items[2]); }
show(values.slice());show(values.slice(1, 4));show(values.slice(-3, -1));
show(values.slice(1.9, 4.9));show(values.slice(-2.9));show(values.slice(4, 1));
show(values.slice(undefined, undefined));show(values.slice(0 / 0, 3));
show(values.slice(-1 / 0, 2));show(values.slice(1 / 0));
const copied = values.slice(1, 3);
copied[0] = 99;
console.log(values[1], copied[0]);
const holes = [1, , undefined, 4];
const holeCopy = holes.slice(1, 4);
show(holeCopy);
const point = {x: 1};
const references = [point, point].slice(1);
references[0].x = 9;
console.log(point.x);
const sparse: number[] = [];
sparse[4294967294] = 7;
show(sparse.slice(-2));
const generic: any = [1, "hello", true, null];
show(generic.slice(1, 4));show(generic.slice("1", "3"));
show(generic.slice(0, null));
console.log("A🦀BC".slice(1, 3), "A🦀BC".slice(-2), "hello".slice(1, 4));
console.log("A🦀B".slice(1, 2).length, "A🦀B".slice(2, 3).length);
console.log("hello".slice(1 / 0), "hello".slice(0 / 0, 2));
const bounds: [number, number] = [1, 3];
show(values.slice(...bounds));
console.log("hello".slice(...bounds));
function change(items: number[]) { items[1] = 8; return 1; }
show(values.slice(change(values), 3));
const state = {x: 0};
function extra(state: {x: number}) { state.x++; return 99; }
show(generic.slice(1, 3, extra(state)));
console.log(state.x);
const special = [0 / 0, -0, undefined].slice();
console.log(special[0], 1 / special[1]!, special[2]);
function shrink(items: number[]) { items.length = 2; return 0; }
show(values.slice(shrink(values), 4));

type WideNumber = { value: number | string };
type DynamicField = { value: any };
type UnknownField = { value: unknown };
function readWide(point: WideNumber) { console.log(point.value); return point; }
function writeWide(point: WideNumber) { point.value = 9; return point; }
function readDynamic(point: DynamicField) { console.log(point.value); return point; }
function readUnknown(point: UnknownField) { console.log(point.value); return point; }
const point = { value: 2, extra: 7 };
console.log(readWide(point) === point, writeWide(point) === point, point.value, point.extra);
console.log(readDynamic(point) === point, readUnknown(point) === point);
const booleanPoint = { value: true };
console.log(readDynamic(booleanPoint) === booleanPoint, readUnknown(booleanPoint) === booleanPoint);
function reference(point: DynamicField) { point.value = "changed"; return point; }
const text = { value: "original" };
console.log(reference(text) === text, text.value);
function references(point: { child: unknown; values: unknown }) { return point; }
const child = { x: 3 };
const array = [1, 2];
const holder = { child, values: array };
const wideParent = references(holder);
console.log(wideParent === holder, wideParent.child === child, wideParent.values === array);
function fresh(point: WideNumber) { point.value = "text"; console.log(point.value); return point; }
console.log(fresh({ value: 2 }).value);
const annotated: WideNumber = { value: 3 };
annotated.value = "four";
console.log(annotated.value);
function optional(point: { value?: number }) { console.log(point.value); point.value = 5; return point.value; }
console.log(optional({ value: undefined }));
const absent: { value?: number } = { value: undefined };
console.log(absent.value);
function nested(point: { child: WideNumber }) { point.child.value = "nested"; return point.child.value; }
console.log(nested({ child: { value: 2 } }));

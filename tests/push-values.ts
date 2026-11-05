const objects: {x: number}[] = [];
console.log(objects.push({x: 1}), objects.push({x: 2}, {x: 3}), objects.push());
console.log(objects[0].x, objects[2].x);
const text: string[] = [];
console.log(text.push("a"), text.push("b", "c"), text.push(), text[2]);
const dynamic: any[] = [];
console.log(dynamic.push(undefined), dynamic.push(NaN), dynamic.push("text"), dynamic.length);
console.log(dynamic[0], Number.isNaN(dynamic[1]), dynamic[2]);
function append(values: any[]): number { values.push(8); return 2; }
console.log(dynamic.push(append(dynamic)), dynamic[3], dynamic[4]);
function build(count: number): number {
    const result: any[] = [];
    for (let i=0;i<count;i++) {
        if (i===3) break;
        result.push(append(result));
    }
    console.log(result.length, result[0], result[1]);
    return result.length;
}
console.log(build(0), build(20));
function shrink(values: any[], value: number): number { values.length=0; return value; }
const reset: any[] = [1];
for(let i=0;i<3;i++) reset.push(shrink(reset,i));
console.log(reset.length, reset[0]);

const direct: any = [1, 2];
direct[0] = "direct";
console.log(direct[0]);
function edit(value: any) { value[0] = "argument"; return value[0]; }
console.log(edit([1, 2]));
function create(): any { return [1, 2]; }
const result = create();
result[0] = "returned";
console.log(result[0]);
const unknown: unknown = [1, 2];
const view = unknown as any;
view[0] = "unknown";
console.log(view[0]);

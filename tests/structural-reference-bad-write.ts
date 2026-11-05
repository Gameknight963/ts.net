function replace(point: { value: any }) { point.value = 42; }
const point = { value: "original" };
replace(point);
console.log(point.value);

function replace(point: { value: number | string }) { point.value = "wrong"; }
const point = { value: 2 };
replace(point);
console.log(point.value);

const point = { value: 1 };
const view: any = point;
view.value = "wrong";
console.log(point.value);

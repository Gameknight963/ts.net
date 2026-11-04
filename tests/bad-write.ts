const point = { x: 1 };
const erased: any = point;
erased.x = "wrong";
console.log(point.x);

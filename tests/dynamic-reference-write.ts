const point = { text: "hello" };
const dynamic: any = point;
dynamic.text = 3;
console.log(point.text);

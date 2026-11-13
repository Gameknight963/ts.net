class Point {x = 0;}
const erased: unknown = 42;
const point = erased as Point;
console.log(point.x);

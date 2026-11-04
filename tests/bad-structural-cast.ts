type Point = { x: number; y: number };
const erased: unknown = { x: 1 };
const point = erased as Point;
console.log(point.y);

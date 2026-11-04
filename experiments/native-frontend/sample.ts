interface Point { x: number; y: number; }

function identity<T>(value: T): T { return value; }

const point = identity({ x: 3, y: 4 });
let count = point.x + 1;
const values = [1, 2, 3].map(value => value * 2);
const selected = values.length > 0 ? point : "empty";

type Optional<T> = { [K in keyof T]?: T[K] };
const optional: Optional<Point> = { x: 1 };
const label = `${point.x},${point.y}`;

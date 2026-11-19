export {};
function identity<T>(value: T): T {return value;}
const point = identity({x: 3, y: 4});
const count = point.x + point.y;
const values = [1, 2, 3].map(value => value * 2);
const selected = Math.random() > 0.5 ? count : 'text';
type OptionalPoint = {[Key in keyof typeof point]?: (typeof point)[Key]};
const optional: OptionalPoint = {x: 1};
const label: string = `count: ${count}`;

interface Point { x: number; y: number; }
type HasX = { x: number };

function magnitudeSquared(p: Point): number {
    return p.x * p.x + p.y * p.y;
}

function moveX(p: HasX, amount: number): void {
    p.x += amount;
}

// A generated class implements both interfaces. The narrower view shares
// the original object, including its extra property and mutable fields.
const point = { x: 3, y: 4, label: "origin" };
console.log(magnitudeSquared(point));
moveX(point, 2);
console.log(point.x, point.label);

const erased: unknown = point;
const restored = erased as Point;
restored.y = 12;
console.log(magnitudeSquared(point));
console.log(point === restored);

const flexible: any = point;
console.log(flexible.x + 1);
flexible.x = 8;
console.log(point.x);
console.log(flexible.missing);

const text: any = "answer: ";
console.log(text + 42);

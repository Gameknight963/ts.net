function kernel(count: number, seed: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const point = { x: i, y: seed };
        if (Object.hasOwn(point, "x") && !Object.hasOwn(point, "missing")) {
            if (i % 2 === 0) point.x += 1;
            total += point.x + point.y;
        }
    }
    return total;
}
console.log(kernel(100, 3));
function escaping(value: number) { return { x: value }; }
console.log(escaping(7).x);
function once(state: { count: number }) { state.count++; return state.count; }
function initializer() {
    const state = { count: 0 };
    const point = { value: once(state) };
    console.log(Object.hasOwn(point, "value"), Object.hasOwn(point, "absent"), state.count);
}
initializer();

function defaultObject(point = { x: 1 }) {
    const alias = point;
    console.log(Object.hasOwn(point, "x"), Object.hasOwn(alias, "x"), alias.x);
}
defaultObject();
defaultObject({ x: 8 });

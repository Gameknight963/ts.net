export {};
for (const callback of [Reflect.deleteProperty]) console.log(typeof callback);
function work(): number {
    let total = 0;
    for (let i = 0; i < 100000; i++) {
        const point = {x: i, y: i + 1};
        total += point.x + point.y;
    }
    return total;
}
console.log(work());

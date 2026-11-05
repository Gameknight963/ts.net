function kernel(count: number): number {
    let total = 0;
    for(let i=0;i<count;i++) {
        const values: {x: number}[] = [];
        values.push({x: i});
        total += values.length;
    }
    return total;
}
console.log(kernel(1000));
function reassigned(count: number): number {
    let values: {x: number}[] = [];
    let total = 0;
    for(let i=0;i<count;i++) {
        values = [];
        values.push({x: i});
        total += values[0].x;
    }
    return total;
}
console.log(reassigned(1000));

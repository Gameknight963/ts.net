function kernel(count: number,seed: number): number {
    const values=[1,2,3,seed];
    let total=0;
    for(let i=0;i<count;i++){values[3]=i;total+=values.at(-1)!;}
    return total;
}
console.log(kernel(10, 0));

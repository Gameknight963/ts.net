function kernel(count:number,seed:number):number {
    const text='abcdefghijklmnopqrstuvwxyz';let total=seed;
    for(let i=0;i<count;i++)total+=text.charCodeAt(i%26)!;
    return total;
}
console.log(kernel(100, 0));

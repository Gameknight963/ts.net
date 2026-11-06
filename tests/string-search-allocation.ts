function kernel0(count:number,seed:number):number {
    const text='alpha beta needle delta alpha beta needle delta';let total=seed;
    for(let i=0;i<count;i++)total+=text.indexOf(i&1?'needle':'missing',(i+seed)&31);
    return total;
}
function kernel1(count:number,seed:number):number {
    const text='alpha beta needle delta alpha beta needle delta';let total=seed;
    for(let i=0;i<count;i++)total+=(text.includes(i&1?'needle':'missing',(i+seed)&31)?1:0);
    return total;
}
function kernel2(count:number,seed:number):number {
    const text='alpha beta needle delta alpha beta needle delta';let total=seed;
    for(let i=0;i<count;i++)total+=text.lastIndexOf(i&1?'needle':'missing',(i+seed)&31);
    return total;
}
console.log(kernel0(257,0),kernel1(257,0),kernel2(257,0));

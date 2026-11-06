function single(count:number,seed:number):number {
    const text='abcdefghijklmnop';let total=seed;
    for(let i=0;i<count;i++){const start=i&15;total+=text.substring(start,start+1)[0].charCodeAt(0);}
    return total;
}
function full(count:number,seed:number):number {
    const text='abcdefghijklmnop';let total=seed;
    for(let i=0;i<count;i++)total+=text.slice(0,Infinity).length;
    return total;
}
function copy(count:number,seed:number):number {
    const text='abcdefghijklmnop';let total=seed;
    for(let i=0;i<count;i++){const start=i&7;total+=text.substring(start,start+4).charCodeAt(0);}
    return total;
}
console.log(single(100,0),full(100,0),copy(100,0));
function fractional(count:number,seed:number):number {
    const text='abcdefghijklmnop';let total=seed;
    for(let i=0;i<count;i++){const value=text[(i&31)/2];if(value!==undefined)total+=value.charCodeAt(0);}
    return total;
}
console.log(fractional(100,0));

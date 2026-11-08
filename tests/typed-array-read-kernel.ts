function make():Float64Array {
    const values=new Float64Array(1024);
    for(let index=0;index<values.length;index++)values[index]=index;
    return values;
}
function sum(values:Float64Array):number {
    let result=0;
    for(let pass=0;pass<100;pass++)for(let index=0;index<values.length;index++)result+=values[index];
    return result;
}
console.log(sum(make()));

export {};
const remove=Reflect.deleteProperty;
function erase(first:boolean):void {
    const record:any={first:1,second:2,keep:3};
    const firstKey="first";
    const key=first?firstKey:"second";
    console.log(remove.call(Reflect,record,key),Object.keys(record).join(","),record.keep);
}
erase(true);
erase(false);
const keyAlias="first" as "second";
const record:any={first:4,second:5};
console.log(Reflect.deleteProperty(record,keyAlias),Object.keys(record).join(","));
function work():number {
    let sum=0;
    for(let index=0;index<100000;index++){
        const point={x:index,y:index+1};
        sum+=point.x+point.y;
    }
    return sum;
}
console.log(work());

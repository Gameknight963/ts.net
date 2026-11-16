export {};
function erase(first:boolean,second:boolean):void {
    const record:{first?:number,second?:number,third?:number,keep:number}={first:1,second:2,third:3,keep:4};
    const firstKey="first";
    const choice=first?firstKey:second?"second":"third";
    const alias=choice;
    console.log(delete record[alias],Object.keys(record).join(","),record.keep);
}
erase(true,false);
erase(false,true);
erase(false,false);
let effects=0;
const sideEffects:{first?:number,second?:number}={first:1,second:2};
console.log(delete sideEffects[(effects++===0)?"first":"second"],effects,Object.keys(sideEffects).join(","));
const assertedKey=(effects===1?"first":"second") as "third";
const asserted:any={first:5,second:6,third:7};
console.log(delete asserted[assertedKey],Object.keys(asserted).join(","));
function work():number {
    let sum=0;
    for(let index=0;index<100000;index++){
        const point={x:index,y:index+1};
        sum+=point.x+point.y;
    }
    return sum;
}
console.log(work());

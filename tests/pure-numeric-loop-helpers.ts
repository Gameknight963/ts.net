export {};
function identity(value:number):number { return value; }
function twice(value:number):number { return identity(value) * 2; }
function arithmetic(value:number, offset:number):number { return twice(value + offset) / 2 - offset; }
function sum(input:number[]):number {
    let result=0;
    for(let i=0;i<input.length;i++) result+=arithmetic(input[i], 3);
    return result;
}
console.log(sum([1,2,3]),sum([]),sum([NaN]),sum([Infinity]));
function sumTyped(input:Float64Array):number {
    let result=0;
    for(let i=0;i<input.length;i++) result+=arithmetic(input[i],3);
    return result;
}
console.log(sumTyped(new Float64Array([4,5,6])));
const sparse:number[]=[1,,3] as number[];
console.log(sum(sparse));
const accessor=[1,2,3];let reads=0;
Object.defineProperty(accessor,'1',{get(){reads++;accessor.length=2;return 7;}});
console.log(sum(accessor),reads);
const changing=[1,2,3];
function mutate(value:number):number { changing.length=1;return value; }
let total=0;for(let i=0;i<changing.length;i++)total+=mutate(changing[i]);
console.log(total,changing.length);
let replacement=(value:number):number=>value;
const replaceInput=[1,2,3];total=0;
for(let i=0;i<replaceInput.length;i++){total+=replacement(replaceInput[i]);replacement=(value:number)=>value*10;}
console.log(total);
let defaults=0;
function defaulted(value:number,extra:number=(defaults++,5)):number{return value+extra;}
total=0;for(let i=0;i<replaceInput.length;i++)total+=defaulted(replaceInput[i]);
console.log(total,defaults);
function recursive(value:number):number{return value>0?recursive(value-1):value;}
total=0;for(let i=0;i<replaceInput.length;i++)total+=recursive(replaceInput[i]);
console.log(total);
function captured(value:number):number{return value+total;}
total=0;for(let i=0;i<replaceInput.length;i++)total+=captured(replaceInput[i]);
console.log(total);
const throwing=[1,2,3];total=0;
function fail(value:number):number{if(value===2)throw new Error('stop');return value;}
try{for(let i=0;i<throwing.length;i++)total+=fail(throwing[i]);}catch(error){console.log((error as Error).message,total);}

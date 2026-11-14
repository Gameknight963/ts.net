export {};
function number(value:any):number{return (value as number)*2;}
for(const value of [0,-0,NaN,Infinity,-Infinity,1.5,undefined]) {
 const result=number(value);console.log(result,Object.is(result,-0));
}
let visits=0;
function next():any{visits++;return 7;}
console.log((next() as number)*3,visits);
async function later():Promise<any>{return undefined;}
async function run(){console.log((await later() as number)*2);}
run();

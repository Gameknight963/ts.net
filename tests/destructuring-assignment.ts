export {};
let a:any,b:any;[a,b]=[1,2];console.log("array",a,b);
[a=3,b=4]=[undefined,null];console.log("defaults",a,b);
[a,...b]=[1,2,3];console.log("rest",a,b.join(","));
({x:a,y:b=5}={x:4});console.log("object",a,b);
let rest:any;({x:a,...rest}={x:1,y:2,z:3});console.log("object rest",a,JSON.stringify(rest));
const holder:any={};[holder.value,holder["other"]]=[6,7];console.log("properties",holder.value,holder.other);
let effects="";
function target():any{effects+="T";return holder;}
function key():string{effects+="K";return "value";}
function* source():Generator<any>{try{effects+="N";yield undefined;effects+="N";yield 2;}finally{effects+="C";}}
[target()[key()]=(()=>{effects+="D";return 8;})()]=source();console.log("order",effects,holder.value);
const original=[9,10];console.log("identity",([a,b]=original)===original,a,b);
const object={x:11};console.log("object identity",({x:a}=object)===object,a);
[[a],{x:b}]=[[12],{x:13}];console.log("nested",a,b);
let fn:any;[fn=()=>0]=[];console.log("name",fn.name);
let called=0;[a=(()=>{called++;return 1;})()]=[null];console.log("lazy",a,called);
effects="";try{[target()[key()]]={*[Symbol.iterator](){try{effects+="N";yield 1;}finally{effects+="C";}}};}catch(error){console.log("failure",(error as Error).name);}console.log("iterator",effects);
async function main():Promise<void>{let value:any;[value=await Promise.resolve(14)]=[];console.log("await",value);}
main().catch(error=>console.log("failure",String(error)));

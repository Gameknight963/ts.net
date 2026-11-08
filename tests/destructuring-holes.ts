export {};
function source():Iterable<number>{let index=0;return {[Symbol.iterator](){return {next(){const current=index++;console.log("next",current);return {get done(){console.log("done",current);return current>=3;},get value(){console.log("value",current);return current;}};},return(){console.log("close");return {done:true,value:undefined};}};}};}
const [,value]=source();console.log("declaration",value);
let assigned=0;[,assigned]=source();console.log("assignment",assigned);
async function main():Promise<void>{const [,value=await Promise.resolve(9)]=source();console.log("async declaration",value);[,assigned=await Promise.resolve(10)]=source();console.log("async assignment",assigned);}
main().catch(error=>console.log("failure",String(error)));

let reads=0;const array=[,2];const proto=Object.create(Array.prototype,{"0":{get(){reads++;return 7;}}});Object.setPrototypeOf(array,proto);
const [,tail]=array;console.log("array getter",reads,tail);
const previous=Object.getPrototypeOf(Array.prototype);let literalReads=0,literalValue=0;
try{Object.setPrototypeOf(Array.prototype,Object.create(previous,{"1":{get(){literalReads++;return 8;}}}));const [first,,last]=[0,,2];literalValue=first!+last!;}finally{Object.setPrototypeOf(Array.prototype,previous);}
console.log("literal getter",literalReads,literalValue);
let closeCalls=0;
const poison={ [Symbol.iterator](){return {next(){return {done:false,get value(){throw new Error("unused value");}};},return(){closeCalls++;return {done:true,value:undefined};}};}};
const [,]=poison;console.log("unused getter",closeCalls);

async function errors():Promise<void>{
 const iteratorPrototype:any=Object.getPrototypeOf([].values());const previous=iteratorPrototype.return;let closed=0;
 iteratorPrototype.return=function(){closed++;return {done:true};};
 try{
  const throwing=[,2];Object.setPrototypeOf(throwing,Object.create(Array.prototype,{"0":{get(){throw new Error("next failed");}}}));
  try{const [,value=await Promise.resolve(3)]=throwing;console.log("unexpected",value);}catch(error){console.log("step failure",(error as Error).message,closed);}
 }finally{if(previous===undefined)delete iteratorPrototype.return;else iteratorPrototype.return=previous;}
}
errors().catch(error=>console.log("failure",String(error)));

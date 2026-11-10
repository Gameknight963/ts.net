export {};
const input=[1,2,3];
console.log('zero', input.map(()=>8).join(','));
console.log('one', input.map(value=>value*2).join(','));
const withIndex=(value:number,index:number):number=>value+index;
Object.defineProperty(withIndex,'length',{value:0});
console.log('index', input.map(withIndex).join(','));
let offset=4;
const captured=(value:number):number=>{offset++;return value+offset;};
console.log('closure',input.map(captured).join(','),offset);
function argumentCount(value:number):number{return arguments.length*100+value;}
console.log('arguments',input.map(argumentCount).join(','));
function third(value:number,index:number,array:number[]):number{return value+index+array.length;}
console.log('source',input.map(third).join(','));
function receiver(this:{factor:number},value:number):number{return this.factor*value;}
console.log('receiver',input.map(receiver,{factor:3}).join(','));
function sum(left:number,right:number):number{return left+right;}
console.log('bound',input.map(sum.bind(undefined,10)).join(','));
console.log('native variadic',input.map(Math.max as (value:number)=>number).join(','));
console.log('defaults',[1,undefined,3].map((value=8)=>value*2).join(','));
const shrinks=[1,2,3];
const shrunk=shrinks.map((value,index)=>{if(index===0)shrinks.length=1;return value;});
console.log('shrink',shrunk.length,shrunk.join(','));
const grows=[1,2,3];
const grown=grows.map((value,index)=>{
 if(index===0){for(let j=0;j<100;j++)grows.push(j);grows[1]=42;}
 return value;
});
console.log('grow',grown.join(','),grows.length);
let reads=0;
const accessors=[1,2,3];
const accessed=accessors.map((value,index)=>{
 if(index===0)Object.defineProperty(accessors,'1',{get(){reads++;return 7;},configurable:true});
 return value+index;
});
console.log('getter',accessed.join(','),reads);
const holes=[1,2,3];delete holes[1];
const prototype=Object.create(Array.prototype);
Object.defineProperty(prototype,'1',{get(){reads++;return 9;},configurable:true});
Object.setPrototypeOf(holes,prototype);
console.log('inherited',holes.map((value,index)=>value+index).join(','),reads);
const generic={0:2,1:4,length:2};
console.log('array like',Array.prototype.map.call(generic,(value:number,index:number)=>value+index).join(','));
const filterSource=[1,2,3];
const filtered=filterSource.filter((value,index)=>{filterSource[index]=99;return value%2===1;});
console.log('filter original values',filtered.join(','),filterSource.join(','));
let predicateCalls=0;
console.log('short circuit',input.every(value=>{predicateCalls++;return value<2;}),predicateCalls);
console.log('find',input.find(value=>value>1),input.findIndex(value=>value>1));
console.log('native predicate',[NaN,Infinity,-0,3].filter(Number.isFinite).join(','));
const contextual:(value:number)=>unknown=value=>value>1;
console.log('contextual boolean',input.map(contextual).join(','));
console.log('mixed predicate',input.filter(value=>value===2?false:{ok:true}).join(','));
console.log('contextual direct',typeof contextual(1),contextual(2));
console.log('strings',input.map(value=>'n'+value).join(','));
const objects=input.map(value=>({value}));
console.log('objects',objects[0].value,objects[2].value);
let visited=0;input.forEach((value,index)=>{visited+=value+index;});
console.log('forEach',visited);
console.log('async predicate',input.filter(async value=>value>1).join(','));
Promise.all(input.map(async value=>value*2)).then(values=>console.log('async mapper',values.join(',')));
function target(value:number):number{return new.target===undefined?value:99;}
console.log('new target',input.map(target).join(','));
function rest(value:number,...others:any[]):number{return value+others.length*100;}
console.log('rest',input.map(rest).join(','));
console.log('default index',input.map((value:number,index=42)=>value+index).join(','));
try {input.map(value=>{if(value===2)throw new Error('mapper');return value;});}
catch(error){console.log('callback error',(error as Error).message);}
const numericTruth = [0, -0, NaN, 2, 3];
console.log('numeric predicates', numericTruth.filter(value=>value).join(','),
 numericTruth.every(value=>value),numericTruth.some(value=>value),
 numericTruth.find(value=>value),numericTruth.findIndex(value=>value));
console.log('missing numeric result',input.filter((value:number):number|undefined=>value===2?undefined:value).join(','));
const findMutation=[1,2,3];
console.log('find original value',findMutation.find(value=>{findMutation[0]=99;return value;}),findMutation[0]);
const predicateGetter=[1,2,3];
let predicateReads=0;
console.log('predicate getter',predicateGetter.every((value,index)=>{
 if(index===0)Object.defineProperty(predicateGetter,'1',{get(){predicateReads++;return 0;},configurable:true});
 return value;
}),predicateReads);
const visitRemoved=[1,2,3];
let removalVisits=0;
visitRemoved.find((value,index)=>{removalVisits++;if(index===0)visitRemoved.length=1;return value===undefined;});
console.log('find removed slot',removalVisits);
{
    const values=[2,4,6];let total=0;
    console.log('source parameter',values.map((value,index,array)=>{
        if(array!==values)throw 'wrong source';return value+array[index]+index;
    }).join(','));
    values.forEach((value,index,array)=>{total+=value+array[index];});
    console.log('source captured total',total);
    const changing=[1,2,3];
    console.log('source filter original',changing.filter((value,index,array)=>{if(index===0)array[0]=9;return true;}).join(','),changing.join(','));
    const access=[1,2,3];let reads=0,sum=0;
    access.forEach((value,index,array)=>{
        if(index===0)Object.defineProperty(array,'1',{get(){reads++;return 4;},configurable:true});
        sum+=value;
    });
    console.log('source getter fallback',sum,reads);
    console.log('mapper missing third',Array.from([3],(value:number,index:number,third?:unknown)=>third===undefined?value+index:-1).join(','));
    console.log('reduce third index',[1,2,3].reduce((acc:number,value:number,index:any)=>acc+value+index,0));
    let observed=0;
    values.forEach(function(this:{factor:number},value,index,array){observed+=this.factor*value+arguments.length+(array===values?0:100);},{factor:2});
    console.log('source this arguments',observed);
}
{
    const holes=[1,,3];const visited:number[]=[];
    holes.forEach((value,index,array)=>{if(array!==holes)throw 'wrong sparse source';visited.push(index);});
    console.log('source holes',visited.join(','),holes.findIndex((value,index,array)=>array===holes&&value===undefined));
    const arrayLike={0:2,1:3,length:2};let total=0;
    Array.prototype.forEach.call(arrayLike,(value:number,index:number,array:any)=>{
        if(array!==arrayLike)throw 'wrong array-like source';total+=value+index;
    });
    console.log('array-like source callback',total);
}

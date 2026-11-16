export {};
function inline(count:number){
 const values:{x:number;y:number;even:boolean}[]=[];
 for(let i=0;i<count;i++)values.push({x:i,y:i+1,even:(i&1)===0});
 return JSON.stringify(values);
}
function blocked(count:number){
 const values:{x:number;y:number}[]=[];
 for(let i=0;i<count;i++){values.push({x:i,y:i+2});}
 return JSON.stringify(values);
}
for(const count of [0,1,4,1.5,-1,NaN])console.log("inline",count,inline(count),blocked(count));
function configured(count:number,setup:(values:any[])=>void){
 const values:{x:number;y:number}[]=[];
 setup(values);
 for(let i=0;i<count;i++)values.push({x:i,y:i+1});
 return values;
}
let reads=0;
console.log("own getter",JSON.stringify(configured(3,values=>Object.defineProperty(values,"push",{configurable:true,get(){reads++;return Array.prototype.push;}}))),reads);
let emptyReads=0;
console.log("empty",configured(0,values=>Object.defineProperty(values,"push",{get(){emptyReads++;return Array.prototype.push;}})).length,emptyReads);
const prototype:any=Array.prototype;
const originalPush=prototype.push;
let changedCalls=0;
function changeMethod(value:number):number {
 if(value===0)prototype.push=function(this:any,...entries:any[]){changedCalls++;return originalPush.apply(this,entries);};
 return value;
}
try {
 const values:{x:number;y:number}[]=[];
 for(let i=0;i<3;i++)values.push({x:changeMethod(i),y:i+1});
 console.log("argument effects",JSON.stringify(values),changedCalls);
}finally{prototype.push=originalPush;}

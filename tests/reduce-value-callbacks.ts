export {};
const data:(number|string)[]=[1,'two',3];
function reducer(sum:number,value:number|string):number{return sum+(typeof value==='number'?value:value.length);}
console.log('directions',data.reduce(reducer,0),data.reduceRight(reducer,0));
console.log('no initial',(data as any).reduce(reducer),(data as any).reduceRight(reducer));
console.log('metadata',reducer.name,reducer.length,reducer(1,'two'),reducer.call(undefined,2,'three'),reducer.apply(undefined,[3,'four']));
console.log('strings',['a','bc'].reduce((sum:number,value:string):number=>sum+value.length,0));
const objects=[{amount:2},{amount:3}];console.log('objects',objects.reduce((sum:number,value:{amount:number}):number=>sum+value.amount,0));
const missing:(number|string)[]=[1,'skip',3];
console.log('missing result',missing.reduce<number|undefined>((sum:number|undefined,value:any):number|undefined=>value==='skip'?undefined:(sum??0)+Number(value),0));
function observed(sum:number,value:any):number{return arguments.length;}
console.log('arguments',data.reduce(observed,0));
console.log('index',data.reduce((sum:number,value:any,index:number):number=>sum+index,0));
console.log('rest',data.reduce((sum:number,...rest:any[]):number=>sum+rest.length,0));
let defaults=0;console.log('defaults',[undefined,'a'].reduce<number>((sum:number,value:any=(++defaults,'xx')):number=>sum+value.length,0),defaults);
console.log('receiver',data.reduce(function(this:undefined,sum:number,value:any):number{return this===undefined?sum+1:-100;},0));
let alias:(sum:number,value:any,index:number)=>number=reducer;
console.log('alias',data.reduce(alias,0));alias=(sum,value,index)=>sum+index;console.log('reassigned',data.reduce(alias,0));
for(const mutation of ['getter','delete','shrink','reverse']) {
 const changing:any[]=[1,'two',3];let calls=0,gets=0;
 const result=changing.reduce<number>((sum:number,item:any):number=>{
  calls++;
  if(calls===1){if(mutation==='getter')Object.defineProperty(changing,'1',{get(){gets++;return 'later';},configurable:true});if(mutation==='delete')delete changing[1];if(mutation==='shrink')changing.length=1;if(mutation==='reverse')changing.reverse();}
  return sum+(typeof item==='number'?item:item.length);
 },0);
 console.log('mutation',mutation,result,calls,gets);
}

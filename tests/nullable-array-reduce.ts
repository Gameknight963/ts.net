export {};
function values(): (number|undefined)[] {return [1,2,3,4].map(value=>(value&1)!==0?value:undefined);}
let visits: string[]=[];
function combine(sum: number|undefined, value: number|undefined): number {visits.push(value===undefined?'missing':String(value));return(sum??0)+(value??0);}
for(const backwards of [false,true]) {
 const data=values();visits=[];console.log('initial',backwards,backwards?data.reduceRight(combine,0):data.reduce(combine,0),visits.join(','));
 visits=[];console.log('no initial',backwards,backwards?data.reduceRight(combine):data.reduce(combine),visits.join(','));
}
let defaults=0;
console.log('defaults',values().reduce((sum:number=0,value:number=(++defaults,10))=>sum+value,0),defaults);
console.log('missing result',values().reduce<number|undefined>((sum,value)=>value===undefined?undefined:(sum??0)+value,0));
console.log('signed zero',Object.is(values().reduce((_sum,_value)=>-0,0),-0));
console.log('nan',Number.isNaN(values().reduce((_sum,_value)=>NaN,0)));
for(const mutation of ['getter','delete','shrink','reverse']) {
 const data=values();let calls=0,gets=0;
 const result=data.reduce((sum:number,value:number|undefined):number=>{
  calls++;
  if(calls===1) {
   if(mutation==='getter')Object.defineProperty(data,'2',{get(){gets++;return undefined;},configurable:true});
   if(mutation==='delete')delete data[2];
   if(mutation==='shrink')data.length=1;
   if(mutation==='reverse')data.reverse();
  }
  return sum+(value??0);
 },0);
 console.log('mutation',mutation,result,calls,gets);
}
const inherited=values();delete inherited[1];let inheritedGets=0;
const prototype=Object.create(Array.prototype);
Object.defineProperty(prototype,'1',{get(){inheritedGets++;return 7;}});
Object.setPrototypeOf(inherited,prototype);
console.log('inherited',inherited.reduce(combine,0),inheritedGets);
const frozen=Object.freeze(values());console.log('frozen',frozen.reduce(combine,0));

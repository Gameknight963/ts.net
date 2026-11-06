const key={value:1},other={value:1};
const map=new Map<any,any>([[1,'one'],['1','text'],[true,'bool'],[null,'null'],[undefined,'undefined'],[NaN,'nan'],[-0,'zero'],[key,'object']]);
console.log('keys',map.size,map.get(1),map.get('1'),map.get(true),map.get(null),map.get(undefined),map.get(NaN),map.get(0),map.get(key),map.has(other));
console.log('chain',map.set(1,'updated')===map,map.delete('1'),map.delete('missing'),map.size);
for(const [name,value] of map)console.log('entry',typeof name,String(name),value);
const set=new Set<any>([NaN,NaN,-0,0,1,'1',true,null,undefined,key,other,key]);
console.log('set',set.size,set.has(NaN),set.has(0),set.has(key),set.add(2)===set,set.delete(1),set.delete(1));
console.log('aliases',Set.prototype.keys===Set.prototype.values);
for(const [first,second] of set.entries())console.log('set entry',Object.is(first,second),typeof first);
console.log('copies',new Map(map).size,new Set(set).size,Array.from(map.keys()).length,[...set].length);
let log='';
const changing=new Map<number,number>([[1,10],[2,20],[3,30]]);
changing.forEach((value,key,owner)=>{log+=key+':'+value+',';if(key===1){owner.delete(2);owner.set(3,31);owner.set(4,40);}});
console.log('forEach',log);
let setLog='';
const changingSet=new Set([1,2,3]);
changingSet.forEach((value,key,owner)=>{setLog+=value+':'+key+',';if(value===1){owner.delete(2);owner.add(4);}});
console.log('set forEach',setLog);
const cursor=changing.keys();console.log('next',cursor.next().value);changing.clear();changing.set(8,80);console.log('after clear',cursor.next().value,cursor.next().done);changing.set(9,90);console.log('sticky',cursor.next().done);
const churn=new Map<number,number>();for(let i=0;i<200;i++)churn.set(i,i);
const live=churn.keys();console.log('churn first',live.next().value);
for(let i=0;i<190;i++)churn.delete(i);for(let i=200;i<220;i++)churn.set(i,i);
let count=0,sum=0;for(const value of live){count++;sum+=value;}console.log('churn',churn.size,count,sum);
console.log('own keys',Object.keys(map).length,Object.keys(set).length,typeof Map,typeof Set,map instanceof Map,set instanceof Set);
class Derived extends Map<number,number>{override set(key:number,value:number):this{super.set(key,value+1);return this;}label='derived';}
const derived=new Derived([[1,2]]);console.log('derived',derived.get(1),derived.label,derived instanceof Derived,derived instanceof Map);
class DerivedSet extends Set<number>{override add(value:number):this{super.add(value+1);return this;}}
const derivedSet=new DerivedSet([1,2]);console.log('derived set',derivedSet.has(2),derivedSet.has(3),derivedSet instanceof DerivedSet);
try{(Map as any)();}catch(error){console.log('new required',(error as Error).name);}
try{Map.prototype.get.call({} as any,1);}catch(error){console.log('brand',(error as Error).name);}
try{new Map([1] as any);}catch(error){console.log('bad entry',(error as Error).name);}
try{(map as any).size=9;}catch(error){console.log('size readonly',(error as Error).name);}
let receiverTotal=0;const receiver={factor:2};new Map([[2,3]]).forEach(function(this:typeof receiver,value,key){receiverTotal+=this.factor*(value+key);},receiver);console.log('receiver',receiverTotal);
async function asyncValues():Promise<number>{let total=0;for await(const value of new Set([Promise.resolve(2),3]))total+=value;return total;}
asyncValues().then(value=>console.log('async set',value));
Promise.all(new Set([Promise.resolve(1),Promise.resolve(2)])).then(values=>console.log('promise set',values[0],values[1]));

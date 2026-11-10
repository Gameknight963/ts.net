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
{
    const numbers=new Map<number,number>();
    const keys=[NaN,-0,0,Infinity,-Infinity,0.125,Number.MIN_VALUE,Number.MAX_SAFE_INTEGER];
    for(let i=0;i<keys.length;i++)numbers.set(keys[i],i+1);
    for(const key of keys)console.log('numeric get',numbers.get(key));
    console.log('numeric missing',numbers.get(123456));
    numbers.set(undefined as unknown as number,42);
    console.log('undefined numeric key',numbers.get(undefined as unknown as number));
    const get=numbers.get;
    console.log('numeric get call',get.call(numbers,NaN),get.bind(numbers)(Infinity));
    for(const key of keys)console.log('numeric membership',numbers.has(key));
    console.log('undefined membership',numbers.has(undefined as unknown as number));
    console.log('undefined deletion',numbers.delete(undefined as unknown as number),numbers.has(undefined as unknown as number));
    numbers.get=function(this:Map<number,number>,key:number):number {return this.size+key;};
    console.log('overridden numeric get',numbers.get(2));
    const compacted=new Map<number,number>();
    for(let i=0;i<200;i++)compacted.set(i,i+1);
    for(let i=0;i<170;i++)compacted.delete(i);
    console.log('numeric get after compaction',compacted.get(190),compacted.get(10),compacted.size);
    compacted.clear();compacted.set(190,7);
    console.log('numeric get after clear',compacted.get(190),compacted.get(191));
    console.log('numeric delete after clear',compacted.delete(190),compacted.delete(190),compacted.has(190));
    const labels=new Map<number,string>([[1,'one']]);
    console.log('numeric key string value',labels.get(1),labels.get(2));
    const object={value:7};const records=new Map<number,typeof object>([[1,object]]);
    console.log('numeric key object identity',records.get(1)===object);
}
{
    const set=new Set<number>([NaN,-0,0.25,Infinity]);
    for(const key of [NaN,-0,0,0.25,Infinity])console.log('numeric set membership',set.has(key),set.delete(key),set.has(key));
    let lookups=0,argumentsRun=0;
    const custom={value:9,get has(){lookups++;return function(this:{value:number},key:number):boolean{return this.value===9&&arguments.length===1&&key===1;};}};
    console.log('custom keyed method',custom.has(++argumentsRun),lookups,argumentsRun);
    const numeric={get(value:number):number{return value+1;}};
    console.log('custom numeric result',numeric.get(2));
    const invalid:any={has:0};let effects=0;
    try{invalid.has(++effects);}catch(error){console.log('noncallable keyed method',(error as Error).name,effects);}
}
{
    const missing={has(first:number,second?:number):boolean{return first===1&&second===undefined;}};
    const defaults={has(first:number,second:number=7):boolean{return first===1&&second===7;}};
    console.log('keyed missing argument',missing.has(1),defaults.has(1));
    let captured=3;
    const closure={has(value:number):boolean{captured+=value;return captured===4;}};
    console.log('keyed closure',closure.has(1),captured);
    const noArguments={has():boolean{return true;}} as {has(value:number):boolean};
    console.log('keyed unused argument',noArguments.has(4));
    const rest={has(...values:number[]):boolean{return values.length===1&&values[0]===2;}};
    console.log('keyed rest argument',rest.has(2));
    const object={value:7};
    const results={get(value:number):typeof object{return object;},delete(value:number):string{return String(value);}};
    console.log('keyed return values',results.get(3)===object,results.delete(4));
}
{
    const missingFirst={has(value:number=5):boolean{return value===5;}};
    console.log('keyed default first',missingFirst.has(undefined as unknown as number));
    let effect=0;
    const returnsVoid={delete(value:number):void{effect+=value;}};
    console.log('keyed void result',returnsVoid.delete(3),effect);
    const usesArguments={has(value:number):boolean{return arguments.length===1&&arguments[0]===value;}};
    console.log('keyed arguments fallback',usesArguments.has(2));
    const receiver={value:8,has(value:number):boolean{return this.value===value;}};
    console.log('keyed receiver fallback',receiver.has(8));
    const undefinedResult={get(value:number):string|undefined{return value===1?'one':undefined;}};
    console.log('keyed undefined result',undefinedResult.get(2));
}
{
    class SuperMap extends Map<number,string> {
        read(key:number):string|undefined{return super.get(key);}
        contains(key:number):boolean{return super.has(key);}
        remove(key:number):boolean{return super.delete(key);}
    }
    class SuperSet extends Set<number> {
        contains(key:number):boolean{return super.has(key);}
        remove(key:number):boolean{return super.delete(key);}
    }
    const map=new SuperMap([[1,'one']]);const set=new SuperSet([2]);
    console.log('super map methods',map.read(1),map.contains(1),map.remove(1),map.contains(1));
    console.log('super set methods',set.contains(2),set.remove(2),set.contains(2));
}

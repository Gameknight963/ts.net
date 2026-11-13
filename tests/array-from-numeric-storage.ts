export {};
const source=[1,2,3];function copy(){return Array.from(source);}
const basic=copy();console.log('ordinary',Array.isArray(basic),Object.getPrototypeOf(basic)===Array.prototype,basic.constructor===Array,Object.keys(basic).join(','));
const descriptor=Object.getOwnPropertyDescriptor(basic,'1')!;console.log('descriptor',descriptor.value,descriptor.writable,descriptor.enumerable,descriptor.configurable);
const mixed:any[]=copy();const alias=mixed;mixed[0]='text';mixed[1]={value:4};mixed.push(undefined,5);delete mixed[2];console.log('mixed',alias===mixed,JSON.stringify(mixed),Object.keys(mixed).join(','),source.join(','));
mixed.length=2;mixed.length=5;console.log('length',JSON.stringify(mixed),Object.keys(mixed).join(','));
const reversed=copy();console.log('reverse',reversed.reverse()===reversed,reversed.join(','),reversed[0]);reversed[0]=9;console.log('materialized write',reversed.join(','));
const searched=copy();console.log('search',searched.indexOf(2),searched.includes(3),searched.lastIndexOf(1));searched.push(4);console.log('search write',searched.join(','));
console.log('slice',copy().slice(1).join(','),'concat',copy().concat([4,5]).join(','));
const sorted=copy();sorted.sort((a,b)=>b-a);console.log('sort',sorted.join(','));
const getter=copy();Object.defineProperty(getter,'1',{get(){return 7;},enumerable:true,configurable:true});console.log('getter',getter[1],JSON.stringify(getter));
const data=copy();Object.defineProperty(data,'1',{value:8,writable:false});console.log('data',data[1],Reflect.set(data,'1',9),data[1]);
const frozen=copy();Object.freeze(frozen);console.log('frozen',Object.isFrozen(frozen),Reflect.set(frozen,'0',9),frozen[0],frozen.join(','));
const special=Array.from([-0,NaN,Infinity,-Infinity]);console.log('special',Object.is(special[0],-0),Number.isNaN(special[1]),special[2],special[3]);
const holes=Array.from([1,,3]);console.log('holes',Object.hasOwn(holes,1),holes[1]===undefined,Object.keys(holes).join(','));
const missing=Array.from([1,undefined,3]);console.log('undefined',Object.hasOwn(missing,1),missing[1]===undefined);
class Custom extends Array<number>{};const custom=Custom.from(source);console.log('custom',custom instanceof Custom,custom.join(','));
const inherited=Object.create(Array.prototype);inherited[1]=10;const own=[1,2,3];Object.setPrototypeOf(own,inherited);console.log('prototype',Array.from(own).join(','));

const keys=Object.keys;
const record={};for(const key of ['a','b','c','d','e','f','g'])record[key]=key;
console.log('delete',Reflect.deleteProperty(record,'b'),Reflect.deleteProperty(record,'f'),Reflect.deleteProperty(record,'missing'));record.b=2;record.f=6;console.log('order',keys(record).join(','));
delete record.e;record.h=8;delete record.a;record.a=1;console.log('promote',keys(record).join(','));
const clone={...record};delete clone.g;clone.g=7;console.log('clone',keys(clone).join(','),keys(record).join(','));
Object.defineProperty(record,'accessor',{get(){throw Error('Unexpected getter');},configurable:true});console.log('accessor',Reflect.deleteProperty(record,'accessor'),Object.hasOwn(record,'accessor'));
const created=Object.create(null,{locked:{value:1,enumerable:true},removable:{value:1,enumerable:true,configurable:true},writable:{value:1,enumerable:true,writable:true}});
console.log('descriptor-delete',Reflect.deleteProperty(created,'locked'),Reflect.deleteProperty(created,'removable'),Reflect.deleteProperty(created,'writable'),keys(created).join(','));
const spread={...created};spread.locked=2;console.log('descriptor-spread',spread.locked,Reflect.deleteProperty(spread,'locked'),created.locked);
for(let i=0;i<4;i++){
 const array=[1,2,3];if(i===3)array.reverse();Object.setPrototypeOf(array,{'1':99});
 console.log('element',Reflect.deleteProperty(array,'1'),array.length,Object.hasOwn(array,'1'),array[1],keys(array).join(','));
 array[1]=8;console.log('rewrite',array[1],keys(array).join(','),Reflect.deleteProperty(array,'length'));
 array[0];delete array[1];console.log('materialized',array[1],Object.hasOwn(array,'1'));
 array[4000000000]=7;delete array[4000000000];console.log('sparse',array.length,Object.hasOwn(array,'4000000000'));
}

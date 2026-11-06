const values=[1,2,3,4,5];console.log('overlap right',values.copyWithin(1,0,4)===values,values.join(','));
console.log('overlap left',[1,2,3,4,5].copyWithin(0,1).join(','));
console.log('negative',[1,2,3,4,5].copyWithin(-2,-4,-1).join(','));
console.log('empty',[1,2,3].copyWithin(0,2,1).join(','));
console.log('infinite',[1,2,3].copyWithin(-Infinity,1,Infinity).join(','));
const sparse=[,2,,4];sparse.copyWithin(1,0,3);console.log('holes',sparse.join(','),Object.keys(sparse).join(','),sparse.length);
const generic:any={0:'a',2:'c',length:3};console.log('generic',(Array.prototype.copyWithin as any).call(generic,1,0,2)===generic,Object.keys(generic).join(','),generic[0],generic[1],generic[2]);
const proto={get 0():number{console.log('inherited getter');return 8;}};Object.setPrototypeOf(proto,Array.prototype);const inherited:any=[,2,3];Object.setPrototypeOf(inherited,proto);inherited.copyWithin(1,0,1);console.log('inherited',inherited[1],Object.hasOwn(inherited,'1'));
const mutation=[1,2,3];const target:any={valueOf():number{console.log('target conversion');mutation.length=1;return 1;}};const start:any={valueOf():number{console.log('start conversion');return 0;}};const end:any={valueOf():number{console.log('end conversion');return 2;}};mutation.copyWithin(target,start,end);console.log('mutation',mutation.join(','),Object.keys(mutation).join(','));
const accessor:any={length:3,get 0():number{console.log('get zero');return 9;},set 1(value:number){console.log('set one',value);},set 2(value:number){console.log('set two',value);}};(Array.prototype.copyWithin as any).call(accessor,1,0,1);
try{(Array.prototype.copyWithin as any).call(null,0);}catch(e){console.log('null',e instanceof TypeError);}
try{(Array.prototype.copyWithin as any).call('abc',0,1);}catch(e){console.log('readonly',e instanceof TypeError);}
console.log('metadata',Array.prototype.copyWithin.name,Array.prototype.copyWithin.length);
const record={0:1,2:3,length:3,label:'record'};
const base:{label:string}=record;
const view:{0?:number,1?:number,2?:number,length:number}=record;
(Array.prototype.copyWithin as any).call(record,1,0,2);
console.log('structural',base.label,view[0],view[1],view[2],Object.hasOwn(record,'2'),'2' in record,Object.keys(record).join(','));
console.log('values',Object.values(record).join(','));console.log('entries',Object.entries(record).map(pair=>pair.join(':')).join('|'));
const copied={...record};console.log('spread',Object.keys(copied).join(','),Object.hasOwn(copied,'2'),copied[0]);
record[2]=9;console.log('rewrite',record[2],Object.hasOwn(record,'2'),Object.keys(record).join(','));
const huge={4294967295:'a',4294967296:'b',length:4294967297,label:'huge'};
(Array.prototype.copyWithin as any).call(huge,4294967296,4294967294,4294967295);
console.log('large keys',Object.keys(huge).join(','),huge[4294967295],huge[4294967296]);
const numericView:{0?:number,2?:number,length:number}={0:4,2:6,length:3};
const spreadView={...numericView};(Array.prototype.copyWithin as any).call(spreadView,2,1,2);console.log('spread delete',spreadView[0],spreadView[2],Object.keys(spreadView).join(','));
let present=0;for(let i=0;i<3;i++)if(Object.hasOwn(spreadView,'2'))present++;console.log('membership loop',present,Object.keys(spreadView).length);
const countRecord={0:1,length:2,label:'count'};(Array.prototype.copyWithin as any).call(countRecord,0,1,2);
let ownCount=0;for(let i=0;i<5;i++)if(Object.hasOwn(countRecord,'0'))ownCount++;console.log('fixed membership',ownCount,Object.keys(countRecord).length,countRecord[0]);
const contextual:{label:string,4294967295:string,length:number}={length:4294967296,4294967295:'high',label:'context'};console.log('context order',Object.keys(contextual).join(','));
const zeroRecord={0:5,length:2};const zeroProto={get 0():number{return 17;}};Object.setPrototypeOf(zeroRecord,zeroProto);(Array.prototype.copyWithin as any).call(zeroRecord,0,1,2);console.log('prototype after delete',zeroRecord[0],Object.hasOwn(zeroRecord,'0'));
const reverse=[1,2,,4];reverse.reverse();reverse.copyWithin(2,1,3);console.log('reversible',reverse.join(','),Object.keys(reverse).join(','));
class Indexed {length=3;constructor(){(this as any)[0]=10;(this as any)[2]=30;}}
const instance=new Indexed();(Array.prototype.copyWithin as any).call(instance,2,1,2);console.log('class',(instance as any)[0],(instance as any)[2],Object.keys(instance).join(','));
const failing:any={length:3,get 0():number{console.log('copy getter');return 4;},set 1(value:number){console.log('copy setter',value);throw 'set failed';}};
try{(Array.prototype.copyWithin as any).call(failing,1,0,1);}catch(e){console.log('setter error',e);}
const sparseHuge:any=[];sparseHuge[4000000000]=7;sparseHuge[4000000001]=8;sparseHuge.copyWithin(4000000001,3999999999,4000000000);console.log('sparse huge',sparseHuge.length,Object.hasOwn(sparseHuge,'4000000001'),sparseHuge[4000000000]);
const quoted={'0':1,'2':3,length:3,label:'quoted'};const quotedBase:{label:string}=quoted;(Array.prototype.copyWithin as any).call(quoted,2,1,2);console.log('quoted',quotedBase.label,quoted['0'],quoted['2'],Object.keys(quoted).join(','),Object.values(quoted).join(','));
const quotedHuge={'4294967295':'first',label:'middle','4294967296':'last',length:4294967297};(Array.prototype.copyWithin as any).call(quotedHuge,4294967296,4294967294,4294967295);console.log('quoted huge',Object.keys(quotedHuge).join(','),quotedHuge['4294967295'],quotedHuge['4294967296']);

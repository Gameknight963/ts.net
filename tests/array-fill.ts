const values=[1,2,3,4];console.log('identity',values.fill(9,1,3)===values,values.join(','));
console.log('negative',[1,2,3,4].fill(8,-3,-1).join(','));
console.log('empty',[1,2].fill(8,2,1).join(','));
console.log('infinite',[1,2].fill(7,-Infinity,Infinity).join(','));
console.log('fractional',[1,2,3].fill(6,0.9,2.9).join(','));
const sparse=[,2,,];sparse.fill(5);console.log('holes',sparse.join(','),Object.keys(sparse).join(','));
const ref={value:1};const objects=[{},{}];objects.fill(ref);console.log('references',objects[0]===ref,objects[1]===ref);
const generic:any={length:3,0:'a'};console.log('generic',(Array.prototype.fill as any).call(generic,'x',1)===generic,generic[0],generic[1],generic[2],generic.length);
const mutation=[1,2,3];const start:any={valueOf():number{console.log('start conversion');mutation.length=1;return 1;}};const end:any={valueOf():number{console.log('end conversion');return 3;}};mutation.fill(4,start,end);console.log('snapshot',mutation.join(','));
const arraylike:any={get length():number{console.log('length getter');return 2;},set 0(value:number){console.log('setter first',value);},set 1(value:number){console.log('setter second',value);}};(Array.prototype.fill as any).call(arraylike,6);
const inherited:any=[,,];const proto={set 0(value:number){console.log('inherited setter',value);}};Object.setPrototypeOf(proto,Array.prototype);Object.setPrototypeOf(inherited,proto);inherited.fill(7);console.log('inherited',Object.hasOwn(inherited,'0'),Object.hasOwn(inherited,'1'),inherited[1]);
try{(Array.prototype.fill as any).call(null,1);}catch(e){console.log('null',e instanceof TypeError);}
try{(Array.prototype.fill as any).call('abc',1);}catch(e){console.log('readonly',e instanceof TypeError);}
const stringResult:any=(Array.prototype.fill as any).call('abc',1,0,0);console.log('empty string',typeof stringResult,stringResult.length);
try{const bad:any={valueOf(){throw 'range failed';}};[1].fill(2,bad);}catch(e){console.log('range throws',e);}
console.log('metadata',Array.prototype.fill.name,Array.prototype.fill.length);
const mixed:any[]=[1,2];mixed.fill('x');console.log('mixed',mixed.join(','));
const special=[1,2,3];special.fill(NaN,1,2);special.fill(-0,2);console.log('special',Number.isNaN(special[1]),1/special[2]);
const missing=[1,2];(missing.fill as any)();console.log('undefined values',typeof missing[0],Object.hasOwn(missing,'0'),missing.length);
const unchanged=[1,2];const final:any={valueOf():number{console.log('unused end converted');return 1;}};unchanged.fill(9,Infinity,final);console.log('unchanged',unchanged.join(','));
const partial:any={length:3,set 0(value:number){console.log('first write',value);},set 1(value:number){throw 'setter failed';},set 2(value:number){console.log('unexpected write',value);}};
try{(Array.prototype.fill as any).call(partial,8);}catch(e){console.log('setter throws',e);}

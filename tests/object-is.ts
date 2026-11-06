const reference={value:1};const fn=()=>1;
const throwing={valueOf(){throw 'unexpected conversion';},toString(){throw 'unexpected conversion';}};
const values:any[]=[undefined,null,false,true,0,-0,1,-1,NaN,Infinity,-Infinity,'','0','same','same',reference,reference,{},fn,fn,()=>1,throwing];
for(let i=0;i<values.length;i++)for(let j=0;j<values.length;j++)console.log(i,j,Object.is(values[i],values[j]));
console.log('zero',Object.is(-0,0),Object.is(0,-0),Object.is(-0,-0));
const numeric=[0,-0,NaN];console.log('array values',Object.is(numeric[0],numeric[1]),Object.is(numeric[2],NaN));
const object={value:-0};console.log('field',Object.is(object.value,-0));
const is=Object.is;console.log('alias',is(NaN,NaN),is.call(null,-0,0));
console.log('missing',(is as any)(),(is as any)(undefined),(is as any)(1));
console.log('boxed',Object.is(Object(1),1),Object.is(Object('a'),'a'));
console.log('metadata',Object.is.name,Object.is.length,Object.keys(Object).includes('is'));
function declared():number{return 1;}
class Example {}
console.log('identities',Object.is(declared,declared),Object.is(Example,Example),Object.is(Object.is,Object.is));
console.log('nan sources',Object.is(Math.sqrt(-1),NaN),Object.is(Number('invalid'),NaN));
console.log('bound',Object.is(declared.bind(null),declared.bind(null)));
try{new (Object.is as any)(1,1);}catch(e){console.log('not constructor',e instanceof TypeError);}

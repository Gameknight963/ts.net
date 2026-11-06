function cursor(label:string,throwClose=false):any {
    const iterator=new Set([1,2,3]).values() as any;
    iterator.return=function():any{console.log('close',label,this===iterator);if(throwClose)throw 'close failure';return {done:true};};
    return iterator;
}
for(const value of cursor('break')){console.log('break value',value);break;}
function returning():number{for(const value of cursor('return'))return value;return 0;}
console.log('returned',returning());
try{for(const value of cursor('throw',true))throw 'body failure';}catch(error){console.log('throw wins',error);}
try{for(const value of cursor('break error',true))break;}catch(error){console.log('close wins',error);}
for(const value of cursor('exhausted')){if(value===1)continue;}
outer:for(let i=0;i<1;i++){for(const value of cursor('outer continue'))continue outer;}
const overridden=cursor('override next');let calls=0;
overridden.next=()=>({value:++calls,done:calls>2});
for(const value of overridden)console.log('custom next',value);
const [first]=cursor('pattern');console.log('pattern',first);
const []=cursor('empty pattern');
try{const [value=(()=>{throw 'default failure';})()]=Object.assign(cursor('default error',true),{next:()=>({value:undefined,done:false})});console.log(value);}catch(error){console.log('default wins',error);}
try{Array.from(cursor('from',true),()=>{throw 'mapper failure';});}catch(error){console.log('mapper wins',error);}
try{new Map(cursor('constructor',true));}catch(error){console.log('constructor error',(error as Error).name);}
class Broken extends Promise<any>{static override resolve(value?:any):any{throw 'resolve failure';}}
Broken.all(cursor('promise',true)).catch(error=>console.log('promise error',error));
async function asyncBreak():Promise<void>{for(const value of cursor('async break')){await 0;break;}}
asyncBreak().then(()=>console.log('async break done'));
async function forAwaitBreak():Promise<void>{for await(const value of cursor('for await break'))break;}
forAwaitBreak().then(()=>console.log('for await break done'));
async function rejectedDefault():Promise<void>{const iterator=cursor('async pattern',true);iterator.next=()=>({value:undefined,done:false});try{const [value=await Promise.reject('default rejected')]=iterator;console.log(value);}catch(error){console.log('async default wins',error);}}
rejectedDefault().then(()=>console.log('async pattern done'));
async function rejectedValue():Promise<void>{const iterator=cursor('rejected value',true);iterator.next=()=>({value:Promise.reject('value rejected'),done:false});try{for await(const value of iterator)console.log(value);}catch(error){console.log('value wins',error);}}
rejectedValue().then(()=>console.log('rejected value done'));

const names=['Promise','Map','Set','Array','Object','Number','String','Boolean','Math','Reflect','JSON','console','performance'];
const values:any[]=[Promise,Map,Set,Array,Object,Number,String,Boolean,Math,Reflect,JSON,console,performance];
for(let i=0;i<names.length;i++)console.log(names[i],values[i]===(globalThis as any)[names[i]],typeof values[i]);
function shadow(Promise:number,Map:string):string {return Promise+':'+Map;}
console.log('shadowed',shadow(3,'local'));
console.log('fallback',typeof Symbol,typeof RegExp,typeof Date);
const original=Promise.resolve;
let calls=0;
(Promise as any).resolve=(value:any)=>{calls++;return original.call(Promise,value);};
const pending=Promise.resolve(7);
(Promise as any).resolve=original;
pending.then(value=>console.log('replaced member',value,calls));

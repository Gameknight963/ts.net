export {};
function normal(value:number|string):number{return typeof value==='number'?value:value.length;}
function mirrored(value:any):number{return 'number'==typeof value?value:4;}
function inverted(value:any):number{return typeof value!=='number'?4:value;}
function invertedMirror(value:any):number{return 'number'!=typeof value?4:value;}
for(const value of [0,-0,NaN,Infinity,-Infinity,'text',''])console.log('normal',typeof value,normal(value),Object.is(normal(value),-0));
for(const value of [1,undefined,null,false,'text',1n,{},[]])console.log('forms',mirrored(value),inverted(value),invertedMirror(value));
for(const value of [2,'text'])console.log('string add','prefix'+(typeof value==='number'?value:3));
let value:any=1;function change(){value='changed';}
console.log('calls',typeof value==='number'?(change(),value):0);
value=1;console.log('logical calls',typeof value==='number'&&(change(),true)?value:0);
let gets=0;const holder={get value():any{return ++gets===1?1:'getter changed';}};
console.log('getter',typeof holder.value==='number'?holder.value:0,gets);
const a:any=1,b:any='other';console.log('different name',typeof a==='number'?b:0);
for(const item of [1,2n])console.log('bigint branch',typeof item==='number'?item:3n);

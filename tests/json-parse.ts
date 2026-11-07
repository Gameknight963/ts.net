function describe(value:any):string {
    if(value===null)return 'null';
    if(typeof value==='string') {const codes:number[]=[];for(let i=0;i<value.length;i++)codes.push(value.charCodeAt(i));return 's:'+codes.join(',');}
    if(typeof value==='number')return 'n:'+String(value)+':'+String(1/value);
    if(typeof value!=='object')return typeof value+':'+String(value);
    const keys=Object.keys(value);const parts:string[]=[];
    for(const key of keys)parts.push(key+'='+describe(value[key]));
    return (Array.isArray(value)?'array:'+value.length:'object')+'{'+parts.join(';')+'}';
}
for(const text of ['null','true','false','0','-0','1.25','-2.5e-3','1e400','-1e400','1e-4000','-1e-4000','9007199254740993','"text"','"\\uD800\\uDC00\\uD800"','"\\b\\f\\n\\r\\t\\/\\\\\\\""','"\u2028\u2029"','[]','{}',' [1,true,null,"x",[2],{"a":3}] \n','{"b":1,"2":2,"1":3,"b":4}','{"__proto__":1,"__proto__":2,"constructor":3}']) {
    console.log('parse',describe(JSON.parse(text)));
}
for(const text of ['', ' ',undefined,'undefined','NaN','Infinity','+1','01','-01','.1','1.','1e','1e+','--1','true false','nul','TRUE','[1,]','[,1]','[1 2]','{"a":1,}','{a:1}','{"a" 1}','{"a":}','"unterminated','"\\x41"','"\\u123"','"\\uZZZZ"','"\n"','\uFEFF1','1\u00a0','/*x*/1','{}x']) {
    try{JSON.parse(text as any);console.log('unexpected acceptance');}catch(error){console.log('invalid',(error as Error).name);}
}
console.log('coercion',JSON.parse(3 as any),JSON.parse(true as any),JSON.parse(null as any));
let effects='';const input={toString():string {effects+='S';return '{"value":1}';}};
console.log('object input',JSON.parse(input as any).value,effects);
const parser=JSON.parse;
console.log('alias',parser('[1]')[0],parser.name,parser.length,Object.keys(JSON).join(','));
let callCount=0;
const revived=JSON.parse('{"b":2,"1":1,"a":[3,4],"x":null}',function(this:any,key:string,value:any,context?:any):any {
    console.log('revive',key,typeof value,context&&context.source,Object.keys(context||{}).join(','),Object.hasOwn(this,key));callCount++;
    if(key==='0')return undefined;
    if(key==='b')return 20;
    return value;
});
console.log('revived',describe(revived),callCount,Object.hasOwn(revived.a,'0'));
console.log('root removal',JSON.parse('1',()=>undefined));
console.log('nonfunction',JSON.parse('1',3 as any));
const changed=JSON.parse('{"a":1,"b":2,"c":3}',function(this:any,key:string,value:any,context?:any):any {
    if(key==='a'){this.b=9;delete this.c;this.extra=4;}
    console.log('changed source',key,describe(value),context?.source);return value;
});
console.log('changed',describe(changed));
const duplicate=JSON.parse('{"x":1,"x":2}',function(key:string,value:any,context?:any):any {console.log('duplicate source',key,context?.source);return value;});
console.log('duplicate',duplicate.x);
const special=JSON.parse('{"__proto__":{"polluted":true}}');
console.log('prototype',Object.getPrototypeOf(special)===Object.prototype,Object.hasOwn(special,'__proto__'),special.polluted,special.__proto__.polluted);
const depth=10000;let nested:any=JSON.parse('['.repeat(depth)+'0'+']'.repeat(depth));
let levels=0;while(Array.isArray(nested)){levels++;nested=nested[0];}console.log('deep',levels,nested);
let visits=0;JSON.parse('['.repeat(500)+'0'+']'.repeat(500),function(key:string,value:any):any {visits++;return value;});console.log('deep reviver',visits);
try{JSON.parse('[',()=>{throw new Error('reviver ran');});}catch(error){console.log('invalid before reviver',(error as Error).name);}
try{JSON.parse('1',()=>{throw new Error('reviver failure');});}catch(error){console.log('reviver failure',(error as Error).message);}


const replaced=JSON.parse('{"a":1,"b":0}',function(this:any,key:string,value:any,context?:any):any {
    if(key==='a')this.b=Object.create(null,{locked:{value:2,enumerable:true},change:{value:3,enumerable:true,configurable:true}});
    if(key==='locked')return undefined;
    if(key==='change')return 9;
    return value;
});
console.log('descriptors',replaced.b.locked,replaced.b.change,Object.getOwnPropertyDescriptor(replaced.b,'change')!.writable);
function shadow(JSON:any):void {console.log('shadow',JSON.parse('x'));}
shadow({parse:(text:string)=>'custom '+text});

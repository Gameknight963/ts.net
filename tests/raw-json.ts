const rawJSON:any=(JSON as any).rawJSON;
const isRawJSON:any=(JSON as any).isRawJSON;
for(const text of ['null','true','false','0','-0','1.00','1E+4','1e400','123456789012345678901234567890','"text"','"\\uD800"','"\uD800"']) {
    const raw=rawJSON(text);
    const serialized=JSON.stringify(raw)!;
    const codes:number[]=[];for(let i=0;i<serialized.length;i++)codes.push(serialized.charCodeAt(i));
    console.log('raw',raw.rawJSON===text,isRawJSON(raw),Object.getPrototypeOf(raw)===null,codes.join(','));
    console.log('nested',JSON.stringify({value:raw}),JSON.stringify([raw]));
}
for(const text of ['', ' ', '1 ', ' 1','\t1','1\n','[]','{}','[1]','{"x":1}','undefined','NaN','Infinity','01','1.','"unterminated','true false']) {
    try{rawJSON(text);console.log('unexpected acceptance');}catch(error){console.log('invalid',(error as Error).name);}
}
for(const value of [null,true,3,undefined])try{console.log('convert',rawJSON(value).rawJSON);}catch(error){console.log('conversion error',(error as Error).name);}
const raw=rawJSON('12345678901234567890');
const descriptor=Object.getOwnPropertyDescriptor(raw,'rawJSON')!;
console.log('descriptor',Object.keys(raw).join(','),Object.getOwnPropertyNames(raw).join(','),descriptor.value,descriptor.writable,descriptor.enumerable,descriptor.configurable);
try{raw.rawJSON='3';}catch(error){console.log('write',(error as Error).name);}
try{raw.extra=3;}catch(error){console.log('extend',(error as Error).name);}
try{delete raw.rawJSON;}catch(error){console.log('delete',(error as Error).name);}
console.log('delete missing',delete raw.extra);
try{Object.setPrototypeOf(raw,{});}catch(error){console.log('prototype',(error as Error).name);}
console.log('same prototype',Object.setPrototypeOf(raw,null)===raw);
try{Object.assign(raw,{extra:1});}catch(error){console.log('assign',(error as Error).name);}
console.log('unchanged',JSON.stringify(raw),Object.keys(raw).join(','));
const copy={...raw};console.log('spread',isRawJSON(copy),JSON.stringify(copy));copy.rawJSON='3';console.log('spread writable',copy.rawJSON);
for(const value of [null,undefined,3,'raw',{},[],{rawJSON:'1'},Object.create(raw)])console.log('not raw',isRawJSON(value));
console.log('root replacer',JSON.stringify(1,()=>raw));
console.log('replace raw',JSON.stringify(raw,(key:string,value:any)=>isRawJSON(value)?'replacement':value));
console.log('toJSON raw',JSON.stringify({toJSON:()=>raw}));
console.log('filtered',JSON.stringify({a:raw,b:1},['a'],2));
const revived=JSON.parse('{"n":12345678901234567890}',function(key:string,value:any,context?:any):any{return key==='n'?rawJSON(context.source):value;});
console.log('source roundtrip',JSON.stringify(revived));
let calls=0;console.log('object conversion',rawJSON({toString():string {calls++;return '1.00';}}).rawJSON,calls);
for(const name of ['rawJSON','isRawJSON']){const method=(JSON as any)[name];const descriptor=Object.getOwnPropertyDescriptor(JSON,name)!;console.log('metadata',method.name,method.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);}
class ReturningRaw {constructor(){return raw as any;}}
class Fields extends ReturningRaw {value=1;}
try{new Fields();}catch(error){console.log('class field',(error as Error).name);}

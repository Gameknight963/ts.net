function show(label:string,value:any,replacer?:any,space?:any):void {console.log(label,JSON.stringify(value,replacer,space));}
for(const value of [null,true,false,0,-0,1.25,1e21,1e-7,NaN,Infinity,-Infinity,undefined,()=>1,'','"\\\b\f\n\r\t\u0000\u001F','\uD800x\uDC00\uD83D\uDE00','\u2028\u2029',[],{},[1,undefined,()=>1,NaN],{b:2,a:1,missing:undefined,fn:()=>1}])show('value',value);
show('keys',JSON.parse('{"b":1,"2":2,"1":3,"__proto__":4}'));
for(const value of [Object(3),Object('text'),Object(true),new Map(),new Set(),new WeakMap()])show('boxed and collections',value);
const shared={x:1};show('shared',{first:shared,second:shared});
const cycle:any={};cycle.self=cycle;
try{JSON.stringify(cycle);}catch(error){console.log('cycle',(error as Error).name);}
show('removed cycle',cycle,(key:string,value:any)=>key==='self'?undefined:value);
const array:any[]=[1,,3];
(Array.prototype as any)[1]=2;show('sparse inherited',array);delete (Array.prototype as any)[1];show('sparse',array);
let effects='';
const value={get toJSON():any {effects+='G';return function(this:any,key:string):any {effects+='T'+key;return {a:1,b:2};};}};
show('toJSON',value,function(this:any,key:string,value:any):any {effects+='R'+key;return key==='b'?undefined:value;});console.log('order',effects);
show('function toJSON',Object.assign(()=>1,{toJSON:()=>3}));
const object:any={a:1,b:2,c:3};
show('mutation',object,function(this:any,key:string,value:any):any {if(key==='a'){delete this.b;this.d=4;this.c=9;}return value;});
const grows:any[]=[1,2];show('array mutation',grows,function(this:any,key:string,value:any):any {if(key==='0'){this.push(3);delete this[1];}return value;});
for(const list of [['b','a','b'],[2,'1',2],[],[Object('a'),Object(2),true,null,{}]])show('list',{a:1,b:2,'1':3,'2':4},list);
show('list inherited',Object.create({inherited:7},{hidden:{value:8}}),['hidden','inherited']);
show('list nested',{b:{b:1,a:2},a:3},['b','a']);
for(const gap of [0,2,2.9,-1,NaN,Infinity,'..','abcdefghijk',Object(2),Object('x'),true])show('gap',{a:[1,{},[]],b:2},undefined,gap);
const number:any=Object(3);number.valueOf=()=>7;show('number conversion',number);
const text:any=Object('x');text.toString=()=> 'custom';show('text conversion',text);
show('root replaced',undefined,(key:string,value:any)=>key===''?{a:1}:value);
show('omitted root',1,()=>undefined);
try{JSON.stringify({get bad():number {throw new Error('getter failed');}});}catch(error){console.log('getter error',(error as Error).message);}
try{JSON.stringify(1,()=>{throw new Error('replacer failed');});}catch(error){console.log('replacer error',(error as Error).message);}
try{JSON.stringify({toJSON(){throw new Error('toJSON failed');}});}catch(error){console.log('toJSON error',(error as Error).message);}
console.log('metadata',JSON.stringify.name,JSON.stringify.length,Object.keys(JSON).length);
let nested:any=0;for(let i=0;i<1000;i++)nested=[nested];const encoded=JSON.stringify(nested)!;console.log('deep',encoded.length,encoded[0],encoded[encoded.length-1]);
let hash=0;
for(let code=0;code<65536;code++){const text=JSON.stringify(String.fromCharCode(code))!;for(let i=0;i<text.length;i++)hash=(hash*31+text.charCodeAt(i))|0;}
console.log('all code units',hash);
const setupLog:string[]=[];
const list:any[]=[Object('a'),Object(2)];
(list[0] as any).toString=()=>{setupLog.push('list');return 'a';};
const gap:any=Object(2);gap.valueOf=()=>{setupLog.push('gap');return 2;};
const setup={toJSON():any {setupLog.push('toJSON');return {a:1,'2':2};}};
JSON.stringify(setup,list,gap);console.log('setup order',setupLog.join(','));
const cut=JSON.stringify({a:1},undefined,'123456789\uD83D\uDE00')!;
const cutCodes:number[]=[];for(let i=0;i<cut.length;i++)cutCodes.push(cut.charCodeAt(i));console.log('gap code units',cutCodes.join(','));
const descriptor=Object.getOwnPropertyDescriptor(JSON,'stringify')!;console.log('descriptor',descriptor.writable,descriptor.enumerable,descriptor.configurable);

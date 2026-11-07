const tag=Object.prototype.toString;
const locale:any=Object.prototype.toLocaleString;
for(const value of [null,undefined,3,'text',true,Object(3),Object('text'),Object(false),[],Array.prototype,()=>1,Object.getPrototypeOf(()=>1),{},new Error('x'),Error.prototype,new Map(),Map.prototype,new Set(),Set.prototype,new WeakMap(),WeakMap.prototype,new WeakSet(),WeakSet.prototype,Promise.resolve(1),Promise.prototype,JSON,Math,Object.create(null)])console.log('tag',tag.call(value));
function args(value:number):void {console.log('arguments',tag.call(arguments));}args(1);
const map=new Map();Object.setPrototypeOf(map,null);console.log('removed tag',tag.call(map));
console.log('inherited tag',tag.call(Object.create(Map.prototype)));
for(const value of [{},3,'text',true])console.log('locale',locale.call(value));
let effects='';
const returned={value:1};
const source={get toString():any {effects+='G';return function(this:any):any {effects+=this===source?'C':'wrong';return returned;};}};
console.log('result untouched',locale.call(source)===returned,effects);
for(const value of [null,undefined,Object.create(null),{toString:3}])try{locale.call(value);}catch(error){console.log('locale error',(error as Error).name);}
const empty={toString():string{return arguments.length+':'+String(this===empty);}};
console.log('ignored arguments',(locale as any).call(empty,'unused',3));
const array=[1,2];console.log('array text',array.toString(),String(array));
const custom:any=[1,2];custom.join=function(this:any):any {console.log('join receiver',this===custom,arguments.length);return returned;};
console.log('join result',custom.toString()===returned,locale.call(custom)===returned);
custom.join=3;console.log('join fallback',custom.toString());
console.log('generic array',Array.prototype.toString.call({join:()=> 'custom'}),Array.prototype.toString.call({}));
console.log('native function',Math.abs.toString(),Object.prototype.toString.toString());
try{Object.getPrototypeOf(Math.abs).toString.call({});}catch(error){console.log('function receiver',(error as Error).name);}
for(const method of [tag,locale,Array.prototype.toString,Object.getPrototypeOf(Math.abs).toString])console.log('metadata',method.name,method.length);
const object:any={toString:()=> '4'};console.log('conversions',+object,String(object),String([1,2]));


let nameReads=0;
class NameGetter {static get name():string {nameReads++;return 'custom';}}
console.log('function name getter',typeof NameGetter.toString(),nameReads);
const originalTag=(Object.prototype as any).toString;
delete (Object.prototype as any).toString;
console.log('deleted object method','toString' in Object.prototype);
try{locale.call({});}catch(error){console.log('deleted locale',(error as Error).name);}
(Object.prototype as any).toString=originalTag;

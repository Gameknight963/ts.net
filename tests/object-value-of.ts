const valueOf=Object.prototype.valueOf;
const object={value:1};const array=[1,2];const functionValue=()=>1;
for(const value of [object,array,functionValue,new Map(),new Set(),new WeakMap(),Promise.resolve(1),Object(3),Object('text'),Object(false)])console.log('identity',valueOf.call(value)===value);
for(const value of [3,-0,NaN,'text',true,false]) {
    const boxed:any=valueOf.call(value);
    console.log('boxed',typeof boxed,Object.getPrototypeOf(boxed)===(typeof value==='number'?Number.prototype:typeof value==='string'?String.prototype:Boolean.prototype),Object.is(boxed.valueOf(),value));
}
for(const value of [null,undefined])try{valueOf.call(value);}catch(error){console.log('nullish',(error as Error).name);}
const inherited:any={get valueOf(){throw new Error('unexpected lookup');}};
console.log('direct ignores member',valueOf.call(inherited)===inherited);
const child:any=Object.create(object);console.log('inherited method',child.valueOf()===child);
const nullPrototype=Object.create(null);console.log('null prototype',valueOf.call(nullPrototype)===nullPrototype);
const primitive:any=Object(5);primitive.valueOf=valueOf;primitive.toString=()=> '7';console.log('numeric conversion',+primitive);
let effects='';const changing:any={valueOf(){effects+='V';return valueOf.call(this);},toString(){effects+='S';return '3';}};console.log('fallback',+changing,effects);
const descriptor=Object.getOwnPropertyDescriptor(Object.prototype,'valueOf')!;console.log('metadata',valueOf.name,valueOf.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);
const view:any={x:'2',y:10};let conversions=0;
const key:any={toString():string {conversions++;return conversions%2===1?'x':'y';}};
console.log('update key',view[key]++,view.x,view.y,conversions);

const target={base:1,get value():any{return this;}};
const receiver={base:2};
console.log('get receiver',Reflect.get(target,'value')===target,Reflect.get(target,'value',receiver)===receiver,Reflect.get(target,'value',undefined));
console.log('get data',Reflect.get(target,'base',receiver),Reflect.get(target,'missing'));
console.log('native getter',Reflect.get(Map.prototype,'size',new Map([[1,2],[3,4]])));
const inherited=Object.create(target,{hidden:{value:3}});
console.log('has',Reflect.has(inherited,'hidden'),Reflect.has(inherited,'base'),Reflect.has(inherited,'toString'),Reflect.has(inherited,'missing'));
console.log('keys',Reflect.ownKeys(inherited).join(','),Reflect.ownKeys([1,,3]).join(','),Reflect.ownKeys(Object('ab')).join(','));
console.log('prototype',Reflect.getPrototypeOf(inherited)===target,Reflect.getPrototypeOf(Object.create(null)));
const descriptor=Reflect.getOwnPropertyDescriptor(inherited,'hidden')!;
console.log('descriptor',descriptor.value,descriptor.writable,descriptor.enumerable,descriptor.configurable,Reflect.getOwnPropertyDescriptor(inherited,'base'));
let effects='';const key:any={toString():string{effects+='K';return 'base';}};
console.log('key',Reflect.get(target,key),Reflect.has(target,key),Reflect.getOwnPropertyDescriptor(target,key)!.value,effects);
for(const method of ['get','has','ownKeys','getPrototypeOf','getOwnPropertyDescriptor'])for(const target of [null,undefined,3,'text',true]) {
    effects='';try{(Reflect as any)[method](target,key);}catch(error){console.log('invalid',method,(error as Error).name,effects);}
}
function add(this:any,first:number,second:number):number {console.log('call receiver',this.value,arguments.length);return first+second;}
const args={get length():number{effects+='L';return 2;},get 0():number{effects+='0';return 3;},get 1():number{effects+='1';return 4;}};
effects='';console.log('apply',Reflect.apply(add,{value:5},args),effects);
console.log('apply native',Reflect.apply(Math.max,null,[1,3,2]),Reflect.apply(Math.max,null,{length:-1}));
console.log('apply boxed',Reflect.apply((a:any,b:any)=>String(a)+String(b),null,Object('12')));
for(const list of [null,undefined,'12',3])try{Reflect.apply(add,{},list as any);}catch(error){console.log('argument error',(error as Error).name);}
effects='';try{Reflect.apply({} as any,null,args);}catch(error){console.log('target first',(error as Error).name,effects);}
const missing:any={length:2};console.log('missing args',Reflect.apply((a:any,b:any)=>String(a)+':'+String(b),null,missing));
const alias=Reflect;console.log('alias',alias.get(target,'base'),Object.prototype.toString.call(Reflect));
function shadow(Reflect:any):void {console.log('shadow',Reflect.get());}shadow({get:()=> 'local'});
for(const name of ['apply','get','has','ownKeys','getPrototypeOf','getOwnPropertyDescriptor']) {const method=(Reflect as any)[name];const descriptor=Object.getOwnPropertyDescriptor(Reflect,name)!;console.log('metadata',method.name,method.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);}

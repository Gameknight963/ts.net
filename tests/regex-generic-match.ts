export {};
const match=RegExp.prototype[Symbol.match],search=RegExp.prototype[Symbol.search];
for(const flags of ['','g','gu','gv']) {
 let calls=0;const positions:number[]=[];
 const receiver:any={flags,lastIndex:9,exec(this:any,text:string){positions.push(this.lastIndex);return ++calls<4?['']:null;}};
 const result=match.call(receiver,'\ud83d\ude00x');
 console.log('match',flags,result?.join('|'),positions.join(','),receiver.lastIndex);
}
let events='';
const receiver:any={lastIndex:7,get flags(){events+='flags|';return '';},exec(this:any,text:string){events+='exec|';this.lastIndex=8;return {0:'value',index:3};}};
console.log('search',search.call(receiver,'text'),receiver.lastIndex,events);
console.log('match object',(match.call(receiver,'text') as any)[0],events);
for(const result of [null,{},1,undefined]) {
 const value:any={flags:'',exec(){return result;},lastIndex:4};
 try{console.log('result',match.call(value,'x')===result);}catch(error){console.log('result',(error as Error).name);}
}
let order='';const input:any={toString(){order+='text|';return 'input';}};
const observed:any={get flags(){order+='flags|';return '';},get exec(){order+='get exec|';return function(){order+='exec|';return null;};}};
console.log('order',match.call(observed,input),order);
const native:any=/a/g;const proto=Object.create(RegExp.prototype,{flags:{get(){return '';},configurable:true}});
Object.setPrototypeOf(native,proto);console.log('flags override','aba'.match(native)?.join(','),native.lastIndex);
for(const value of [null,undefined,1,'x'])try{match.call(value as any,'x');}catch(error){console.log('receiver',(error as Error).name);}

let state=-0;const steps:string[]=[];
const tracked:any={get lastIndex(){steps.push('get');return state;},set lastIndex(value:number){steps.push(Object.is(value,-0)?'set -0':'set '+value);state=value;},exec(){steps.push('exec');return null;}};
console.log('tracked',search.call(tracked,'x'),Object.is(state,-0),steps.join(','));
const marker=new Error('failure');const failed:any={lastIndex:5,exec(){throw marker;}};
try{search.call(failed,'x');}catch(error){console.log('throw',error===marker,failed.lastIndex);}

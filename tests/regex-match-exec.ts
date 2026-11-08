export {};
const regex:any=/native/;
const result:any=['custom'];result.index=3;result.input='text';
regex.exec=function(this:unknown,text:string){console.log('exec',this===regex,text,this===regex);return result;};
console.log('identity','text'.match(regex)===result);
regex.lastIndex=7;console.log('search','text'.search(regex),regex.lastIndex);
let calls=0;const global:any=/native/g;
global.exec=function(this:any,text:string){calls++;console.log('global call',this===global,text,this.lastIndex);if(calls===3)return null;this.lastIndex++;return [calls===1?'first':'second'];};
console.log('global','input'.match(global)?.join(','),calls,global.lastIndex);
calls=0;const empty:any=/x/g;
empty.exec=function(this:any){console.log('empty index',this.lastIndex);return ++calls<3?['']:null;};
console.log('empty','text'.match(empty)?.join(','),empty.lastIndex);
const bad:any=/x/;bad.exec=()=>1;
for(const search of [false,true])try{console.log(search?'a'.search(bad):'a'.match(bad));}catch(error){console.log('bad',(error as Error).name);}
const marker=new Error('exec');bad.exec=()=>{throw marker;};
try{'a'.match(bad);}catch(error){console.log('throw',error===marker);}
const fallback:any=/a/g;fallback.exec=null;console.log('fallback','aba'.match(fallback)?.join(','));
class Custom extends RegExp {exec(text:string):RegExpExecArray|null{const value:[string]=['subclass'];return text==='yes'?Object.assign(value,{index:2,input:text}):null;}}
console.log('subclass','yes'.match(new Custom('x'))?.[0],'no'.match(new Custom('x')));

const original=RegExp.prototype.exec;let reads=0;
const inherited:any=/a/g;
const prototype=Object.create(RegExp.prototype,{exec:{get(){reads++;return original;},configurable:true}});
Object.setPrototypeOf(inherited,prototype);
console.log('getter','aba'.match(inherited)?.join(','),reads,inherited.lastIndex);
const changing:any=/a/g;changing.exec=function(this:any){this.exec=original;this.lastIndex=1;return ['custom'];};
console.log('changing','aba'.match(changing)?.join(','),changing.lastIndex);

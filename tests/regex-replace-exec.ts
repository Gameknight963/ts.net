export {};
const regex:any=/x/;
regex.exec=function(){return Object.assign(['match','first',undefined],{index:1,input:'ignored',groups:{name:'named'}});};
console.log('template','abcdef'.replace(regex,"$$|$&|$`|$'|$1|$2|$3|$01|$10|$<name>|$<missing>"));
console.log('callback','abcdef'.replace(regex,(...args:any[])=>{console.log('args',JSON.stringify(args));return 'replacement';}));
let events='';let calls=0;const global:any=/x/g;
global.exec=function(this:any,text:string){events+='exec|';if(++calls===3)return null;return Object.assign([calls===1?'b':'d'],{index:calls===1?1:3});};
console.log('global','abcdef'.replace(global,(...args:any[])=>{events+='replace|';return args[0].toUpperCase();}),events);
const replace:any=RegExp.prototype[Symbol.replace];
const generic:any={flags:'',exec(){return {0:'bc',1:'capture',length:2,index:1,groups:undefined};}};
console.log('generic',replace.call(generic,'abcd','[$1][$&]'));
for(const index of [-10,NaN,Infinity,2]) {
 const source:any={flags:'',exec(){return {0:'toolong',length:1,index};}};
 console.log('index',index,replace.call(source,'abc',"[$&][$']"));
}
for(const result of [null,1,undefined]) {
 const source:any={flags:'',exec(){return result;}};
 try{console.log('invalid',replace.call(source,'abc','x'));}catch(error){console.log('invalid',(error as Error).name);}
}
let order='';const input:any={toString(){order+='input|';return 'abc';}};
const replacement:any={toString(){order+='replacement|';return 'x';}};
const observed:any={get flags(){order+='flags|';return '';},get exec(){order+='get exec|';return function(){order+='exec|';return null;};}};
console.log('order',replace.call(observed,input,replacement),order);
const fallback:any=/a/g;fallback.exec=null;console.log('fallback','aba'.replace(fallback,'x'));

for(const flags of ['g','gu','gv']) {
 let calls=0;const positions:number[]=[];
 const source:any={flags,lastIndex:0,exec(this:any){positions.push(this.lastIndex);if(++calls===4)return null;return {0:'',length:1,index:this.lastIndex};}};
 console.log('empty',flags,replace.call(source,'\ud83d\ude00x','-'),positions.join(','));
}
let overlapCalls=0,callbacks=0;
const overlap:any={flags:'g',exec(){if(++overlapCalls===3)return null;return {0:'ab',length:1,index:0};}};
console.log('overlap',replace.call(overlap,'abcd',()=>{callbacks++;return 'X';}),callbacks);
const nullGroups:any={flags:'',exec(){return {0:'a',length:1,index:0,groups:null};}};
try{replace.call(nullGroups,'a','$<name>');}catch(error){console.log('groups',(error as Error).name);}

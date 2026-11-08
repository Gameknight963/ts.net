export {};
const test=RegExp.prototype.test;
for(const result of [null,{},[],undefined,1,false,'match']) {
 const receiver:any={exec(this:unknown,text:string){console.log('call',this===receiver,typeof text,text);return result;}};
 try{console.log('result',test.call(receiver,'input'));}catch(error){console.log('result',(error as Error).name);}
}
const native:any=/a/g;
native.exec=function(this:unknown,text:string){console.log('override',this===native,text);return {};};
console.log('native override',native.test('b'),native.lastIndex);
for(const exec of [undefined,null,3]) {
 native.exec=exec;native.lastIndex=0;console.log('fallback',native.test('a'),native.lastIndex,native.test('b'),native.lastIndex);
 try{test.call({exec} as any,'a');}catch(error){console.log('no matcher',(error as Error).name);}
}
const marker=new Error('exec');
try{test.call({exec(){throw marker;}} as any,'a');}catch(error){console.log('throw',error===marker);}
let order='';
const input:any={toString(){order+='text|';return 'converted';}};
const receiver:any={get exec(){order+='get|';return function(this:unknown,text:string){order+='exec|';return null;};}};
console.log('order',test.call(receiver,input),order);
for(const receiver of [null,undefined,1,'text',true]) {
 try{test.call(receiver as any,input);}catch(error){console.log('receiver',(error as Error).name);}
}

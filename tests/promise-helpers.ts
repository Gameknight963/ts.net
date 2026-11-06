/// <reference lib="es2024.promise" />
/// <reference lib="esnext.promise" />
console.log('start');
const deferred=Promise.withResolvers<number>();
deferred.promise.then(v=>console.log('resolved',v));deferred.resolve(3);deferred.reject('ignored');
const rejected=Promise.withResolvers<number>();rejected.promise.catch(e=>console.log('rejected',e));rejected.reject('reason');rejected.resolve(4);
console.log('record',Object.keys(deferred).join(','),deferred.promise instanceof Promise);
Promise.try(function(this:unknown,a:number,b:number):number{console.log('callback',this===undefined,arguments.length,a,b);return a+b;},2,5).then(v=>console.log('try result',v));
Promise.try(()=>{throw 'callback error';}).catch(e=>console.log('try error',e));
Promise.try(1 as any).catch(e=>console.log('invalid callback',e instanceof TypeError));
Promise.try(()=>Promise.resolve(8)).then(v=>console.log('try promise',v));
class Derived<T> extends Promise<T>{}
const derived=Derived.withResolvers<number>();console.log('derived',derived.promise instanceof Derived);derived.promise.then(v=>console.log('derived value',v));derived.resolve(9);
console.log('derived try',Derived.try(()=>10) instanceof Derived);
console.log('metadata',Promise.withResolvers.name,Promise.withResolvers.length,Promise.try.name,Promise.try.length);
console.log('end');

const {promise,resolve}=Promise.withResolvers<string>();promise.then(v=>console.log('destructured',v));resolve('value');
const thenable:any={get then():unknown {console.log('then getter');return function(fulfill:any):void{console.log('then body');fulfill(11);};}};
Promise.try(()=>thenable).then(v=>console.log('thenable',v));
Promise.try(()=>Promise.reject('returned rejection')).catch(e=>console.log('returned rejection',e));
const getterFailure:any={get then():unknown {throw 'bad then getter';}};
Promise.try(()=>getterFailure).catch(e=>console.log('then getter error',e));
class Recorded {
 constructor(executor:any){console.log('custom constructor');executor((value:any)=>console.log('custom resolve',value),(reason:any)=>console.log('custom reject',reason));}
}
const record=(Promise.withResolvers as any).call(Recorded);console.log('custom record',record.promise instanceof Recorded,Object.keys(record).join(','));record.resolve(12);record.reject('reason');
console.log('custom try',(Promise.try as any).call(Recorded,()=>13) instanceof Recorded);
(Promise.try as any).call(Recorded,()=>{throw 'custom callback error';});
class Broken {constructor(executor:any){throw 'constructor error';}}
try{(Promise.try as any).call(Broken,()=>console.log('unexpected callback'));}catch(e){console.log('constructor error',e);}
try{(Promise.withResolvers as any).call({});}catch(e){console.log('bad constructor',e instanceof TypeError);}
try{(Promise.try as any).call({});}catch(e){console.log('bad try constructor',e instanceof TypeError);}
class BadCapability {constructor(executor:any){executor(1,2);}}
try{(Promise.withResolvers as any).call(BadCapability);}catch(e){console.log('bad capability',e instanceof TypeError);}
Promise.try(function(this:unknown):number{console.log('no args',arguments.length,this===undefined);return 14;}).then(v=>console.log('no args value',v));
class ThrowResolve {constructor(executor:any){executor(()=>{throw 'resolve failed';},()=>console.log('unexpected reject'));}}
try{(Promise.try as any).call(ThrowResolve,()=>16);}catch(e){console.log('resolve throws',e);}
class ThrowReject {constructor(executor:any){executor(()=>console.log('unexpected resolve'),()=>{throw 'reject failed';});}}
try{(Promise.try as any).call(ThrowReject,()=>{throw 'callback';});}catch(e){console.log('reject throws',e);}

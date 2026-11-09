console.log('start');
const promise=new Promise<number>(function(this:unknown,resolve,reject) {console.log('executor',this===undefined,arguments.length);resolve(3);reject('ignored');throw 'ignored throw';});
promise.then(function(this:unknown,value) {console.log('fulfilled',value,this===undefined,arguments.length);return value+1;}).then(value=>console.log('chain',value));
Promise.reject('reason').catch(reason=>{console.log('rejected',reason);return 7;}).then(value=>console.log('recovered',value));
new Promise(()=>{throw 'executor failure';}).catch(reason=>console.log('executor-throw',reason));
Promise.resolve(1).then(()=>{throw 'handler failure';}).catch(reason=>console.log('handler-throw',reason));
const resolved=Promise.resolve(5);console.log('identity',Promise.resolve(resolved)===resolved,resolved instanceof Promise,Object.getPrototypeOf(resolved)===Promise.prototype);
Promise.resolve().then(()=>console.log('microtask A'));queueMicrotask(()=>console.log('microtask B'));Promise.resolve().then(()=>console.log('microtask C'));
const thenable:any={get then():unknown {console.log('then getter');return function(this:unknown,resolve:any,reject:any):void {console.log('then body',this===thenable);resolve(9);reject('ignored');};}};
Promise.resolve(thenable).then(value=>console.log('thenable',value));
const bad:any={get then():unknown {throw 'getter failure';}};Promise.resolve(bad).catch(reason=>console.log('then-getter-throw',reason));
const cycle:any=Promise.resolve(1).then(()=>cycle);cycle.catch((reason:any)=>console.log('cycle',reason instanceof TypeError));
Promise.resolve(4).finally(function(this:unknown) {console.log('finally',this===undefined,arguments.length);return 99;}).then(value=>console.log('finally-value',value));
Promise.reject('original').finally(()=>Promise.resolve(1)).catch(reason=>console.log('finally-reason',reason));
Promise.resolve(1).finally(()=>{throw 'cleanup failure';}).catch(reason=>console.log('finally-throw',reason));
let resolveLater!:(value:number)=>void;
const pending=new Promise<number>(resolve=>{resolveLater=resolve;});pending.then(value=>console.log('later',value));resolveLater(8);
console.log('metadata',Promise.name,Promise.length,Promise.resolve.length,Promise.prototype.then.length,Promise.prototype.catch.length,Promise.prototype.finally.length,queueMicrotask.length,Object.keys(Promise.prototype).length);
console.log('end');
const dynamicPromise:any=Promise;
try {dynamicPromise(()=>{});} catch(error) {console.log('requires-new',error instanceof TypeError);}
try {new dynamicPromise(1);} catch(error) {console.log('invalid-executor',error instanceof TypeError);}
try {Promise.prototype.then.call({});} catch(error) {console.log('invalid-then',error instanceof TypeError);}
try {queueMicrotask(1 as any);} catch(error) {console.log('invalid-microtask',error instanceof TypeError);}
Promise.resolve(6).then(undefined).then(value=>console.log('identity-handler',value));
Promise.reject('forwarded').then(undefined,1 as any).catch(reason=>console.log('thrower-handler',reason));
const genericCatch:any=Promise.prototype.catch;
console.log('generic-catch',genericCatch.call({then(first:unknown,second:unknown):number {console.log('catch-args',first===undefined,second===123);return 7;}},123));
const genericFinally:any=Promise.prototype.finally;
console.log('generic-finally',genericFinally.call({then(first:unknown,second:unknown):number {console.log('finally-args',first===123,second===123);return 8;}},123));
class DerivedPromise<T> extends Promise<T> {label='derived';}
const derived=new DerivedPromise<number>(resolve=>resolve(3));
const derivedChild=derived.then(value=>value+1);
console.log('subclass',derived instanceof DerivedPromise,derived instanceof Promise,derived.label,derivedChild instanceof DerivedPromise,DerivedPromise.resolve(4) instanceof DerivedPromise);
derivedChild.then(value=>console.log('subclass-value',value));
const promiseWithConstructor:any=Promise.resolve(1);promiseWithConstructor.constructor=3;
try {promiseWithConstructor.then();} catch(error) {console.log('bad-constructor',error instanceof TypeError);}
const getThen:any={then(resolve:any):void {resolve(1);throw 'ignored after resolve';}};Promise.resolve(getThen).then(value=>console.log('thenable-throw-after',value));
Promise.resolve({then:1} as any).then(value=>console.log('non-callable-then',value.then));
let unlock!:(value:number)=>void;const locked=new Promise<number>(resolve=>{unlock=resolve;});
new Promise<number>((resolve,reject)=>{resolve(locked);reject('ignored while pending');}).then(value=>console.log('locked-in',value));unlock(11);
Promise.reject('finally original').finally(()=>Promise.reject('finally replacement')).catch(reason=>console.log('finally-replaces',reason));
queueMicrotask(()=>{console.log('nested A');queueMicrotask(()=>console.log('nested B'));});
const resolveMethod:any=Promise.resolve,rejectMethod:any=Promise.reject;
try {resolveMethod(1);} catch(error) {console.log('unbound-resolve',error instanceof TypeError);}
try {rejectMethod.call({},1);} catch(error) {console.log('bad-reject-receiver',error instanceof TypeError);}
let capabilityLog='';
function Capability(this:any,executor:any):void {capabilityLog+='C';executor((value:any)=>{capabilityLog+='R'+value;},(reason:any)=>{capabilityLog+='J'+reason;});this.kind='capability';}
console.log('custom-capability',resolveMethod.call(Capability,4).kind,capabilityLog);
capabilityLog='';console.log('custom-reject',rejectMethod.call(Capability,'x').kind,capabilityLog);
function RetryCapability(this:any,executor:any):void {executor(undefined,undefined);executor(()=>{},()=>{});this.valid=true;}
console.log('retry-capability',resolveMethod.call(RetryCapability,1).valid);
let selfResolve!:(value:unknown)=>void;const selfPromise=new Promise(resolve=>{selfResolve=resolve;});selfResolve(selfPromise);selfPromise.catch(reason=>console.log('resolve-self',reason instanceof TypeError));
Promise.resolve(1).finally(123 as any).then(value=>console.log('ignored-finally',value));
const overriddenThen:any=Promise.resolve(1);overriddenThen.then=function(first:unknown,second:unknown):number {console.log('overridden-catch',first===undefined,typeof second);return 7;};console.log('catch-lookup',overriddenThen.catch(()=>{}));
queueMicrotask(()=>{Promise.reject('handled in microtask').catch(reason=>console.log(reason));});

const queuedOrder: string[] = [];
for (let i = 0; i < 64; i++) {
    Promise.resolve(i).then(value => {queuedOrder.push('p' + value);});
    queueMicrotask(() => {queuedOrder.push('q' + i);});
}
queueMicrotask(() => console.log('mixed queue order', queuedOrder.join(',')));

console.log('start');
Promise.all([Promise.resolve(1),2,Promise.resolve(3)]).then(v=>console.log('all',v.join(',')));
Promise.all([]).then(v=>console.log('all empty',v.length));
Promise.all([Promise.reject('bad'),Promise.resolve(1)]).catch(e=>console.log('all rejected',e));
Promise.allSettled([Promise.resolve(4),Promise.reject('reason'),5]).then(values=>{
 for(const value of values) console.log('settled',value.status,value.status==='fulfilled'?value.value:value.reason);
});
Promise.allSettled([]).then(v=>console.log('settled empty',v.length));
Promise.race([Promise.resolve(6),7]).then(v=>console.log('race',v));
Promise.race([Promise.reject('race bad'),8]).catch(e=>console.log('race rejected',e));
Promise.race([]).then(()=>console.log('unexpected empty race'));
Promise.any([Promise.reject('first'),Promise.resolve(9),10]).then(v=>console.log('any',v));
Promise.any([Promise.reject('one'),Promise.reject('two')]).catch(e=>console.log('any rejected',e instanceof AggregateError,e.errors.join(','),Object.keys(e).length));
Promise.any([]).catch(e=>console.log('any empty',e instanceof AggregateError,e.errors.length));
Promise.all('A\u{1F600}B').then(v=>console.log('string',v.length,v.join('|')));
Promise.all([,1]).then(v=>console.log('holes',v.length,Object.keys(v).join(','),typeof v[0]));
Promise.all(1 as any).catch(e=>console.log('invalid iterable',e instanceof TypeError));
console.log('metadata',Promise.all.name,Promise.all.length,Promise.allSettled.name,Promise.race.length,Promise.any.name);
console.log('end');
class Derived<T> extends Promise<T> {}
const derived=Derived.all([1,2]);console.log('derived',derived instanceof Derived);derived.then(v=>console.log('derived result',v.join(',')));
let getterCalls=0;
class Recorded {
 constructor(executor:any){executor((value:any)=>console.log('record resolve',value.join(',')),(error:any)=>console.log('record reject',error));}
 static get resolve():any {
  console.log('resolve getter',++getterCalls);
  return function(this:unknown,value:number):any {
   console.log('resolve call',this===Recorded,value);
   return {then(fulfill:any,reject:any):void{fulfill(value);fulfill(99);reject('ignored');}};
  };
 }
}
console.log('recorded result',(Promise.all as any).call(Recorded,[1,2]) instanceof Recorded);
class SettledRecorded {
 constructor(executor:any){executor((values:any)=>console.log('record settled',values[0].status,values[0].value,values.length),(e:any)=>console.log('record settled rejected',e));}
 static resolve(value:any):any{return {then(fulfill:any,reject:any):void{fulfill(value);reject('ignored');fulfill(99);}};}
}
(Promise.allSettled as any).call(SettledRecorded,[7]);
const overridden:any=Promise.resolve(1);overridden.then=function(fulfilled:any):void{console.log('overridden then');fulfilled(8);};
Promise.all([overridden]).then(v=>console.log('overridden result',v.join(',')));
class BadResolve extends Promise<any>{static get resolve():any{console.log('bad resolve getter');throw 'resolve getter failed';}}
BadResolve.all([]).catch(e=>console.log('resolve failure',e));
class NonCallableResolve extends Promise<any>{static get resolve():any{return 1;}}
NonCallableResolve.race([]).catch(e=>console.log('noncallable resolve',e instanceof TypeError));
try {(Promise.all as any).call({},[]);}catch(e){console.log('invalid constructor',e instanceof TypeError);}
const thenable:any={then(fulfill:any):void{console.log('thenable body');fulfill(11);}};
Promise.all([thenable,12]).then(v=>console.log('thenable all',v.join(',')));
let rejectFirst!:(value:any)=>void, rejectSecond!:(value:any)=>void;
const first=new Promise((resolve,reject)=>{rejectFirst=reject;}),second=new Promise((resolve,reject)=>{rejectSecond=reject;});
Promise.any([first,second]).catch(e=>console.log('rejection order',e.errors.join(',')));
rejectSecond('second');rejectFirst('first');
const mutation=[1,2];
class Mutating extends Promise<any>{static resolve(value?:any):any{mutation.push(3);mutation.length=1;return Promise.resolve(value);}}
Mutating.all(mutation).then(v=>console.log('mutation',v.join(',')));

function argumentInput(...unused:number[]):void{Promise.all(arguments).then(v=>console.log('arguments',v.join(',')));}
argumentInput(2,4,6);
Promise.allSettled([1,Promise.reject(2)]).then(values=>{for(const v of values)console.log('settled keys',Object.keys(v).join(','));});
class RejectedRecorded {
 constructor(executor:any){executor((v:any)=>console.log('unexpected any resolve',v),(e:any)=>console.log('record any',e instanceof AggregateError,e.errors.join(',')));}
 static resolve(value:any):any{return {then(fulfill:any,reject:any):void{reject(value);reject('duplicate');}};}
}
(Promise.any as any).call(RejectedRecorded,['a','b']);
const getterInput:any=[1,,];const getterPrototype={get 1():number{console.log('element getter');throw 'element failure';}};Object.setPrototypeOf(getterPrototype,Array.prototype);Object.setPrototypeOf(getterInput,getterPrototype);
Promise.all(getterInput).catch(e=>console.log('element failure',e));

const savedResolve = Promise.resolve;
let resolveReads = 0;
Object.defineProperty(Promise, 'resolve', {configurable: true, get() {
    resolveReads++;
    return savedResolve;
}});
Promise.all([3, 4]).then(values => console.log('captured native resolve', resolveReads, values.join(',')));
Object.defineProperty(Promise, 'resolve', {configurable: true, writable: true, value: savedResolve});

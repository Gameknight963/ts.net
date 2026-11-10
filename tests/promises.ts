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

Promise.resolve(4).then(value => value + 1).then(value => console.log('numeric callback', value));
Promise.resolve('text').then((value, suffix = '!') => value + suffix).then(value => console.log('string callback', value));
Promise.resolve({value: 5}).then(({value}) => value + 2).then(value => console.log('object callback', value));
Promise.resolve([2, 3]).then(values => values[0] + values[1]).then(value => console.log('array callback', value));
Promise.resolve<number | undefined>(undefined).then((value = 7) => value * 2).then(value => console.log('default callback', value));
Promise.resolve(5).then(() => 99).then(value => console.log('zero parameter callback', value));
Promise.resolve(2).then((first, second = 5) => first + second).then(value => console.log('optional callback', value));
Promise.resolve(2).then((first, second = 5, third = 7) => first + second + third).then(value => console.log('generic optional callback', value));
Promise.resolve(null).then(() => 'null').then(value => console.log('reference callback', value));
Promise.resolve(8).then((...values: number[]) => values.length + values[0]).then(value => console.log('generic rest callback', value));
Promise.resolve(-3).then(Math.abs).then(value => console.log('native callback', value));
const boundPromiseCallback = ((first: number, second: number) => first * 10 + second).bind(undefined, 3);
Promise.resolve(4).then(boundPromiseCallback).then(value => console.log('bound callback', value));
Promise.resolve<number | string>(3).then(value => typeof value === 'number' ? value + 1 : value.length).then(value => console.log('union callback', value));
Promise.resolve(4).then(value => value > 0).then(value => console.log('boolean callback', value));

Promise.resolve(9n).then(value => value + 1n).then(value => console.log('bigint callback', value.toString()));
const directCallbackSymbol = Symbol('direct');
Promise.resolve(directCallbackSymbol).then(value => value).then(value => console.log('symbol callback', value === directCallbackSymbol));
function directGenericCallback<T>(value: T): T {return value;}
Promise.resolve('generic').then(directGenericCallback).then(value => console.log('generic callback', value));
class SuperThenPromise extends Promise<number> {
    read():Promise<number>{return super.then(value=>value+1);}
}
new SuperThenPromise(resolve=>resolve(4)).read().then(value=>console.log('super then',value));
{
    let gets=0,calls=0;
    const custom={value:7,get then(){gets++;return function(this:{value:number},callback:(value:number)=>number):number{calls++;return callback(this.value)+arguments.length;};}};
    console.log('overridden then',custom.then(value=>value+1),gets,calls);
    const invalid:any={then:0};let effects=0;
    try{invalid.then((effects++,()=>0));}catch(error){console.log('noncallable then',(error as Error).name,effects);}
}
{
    const savedThen=Promise.prototype.then;let overrides=0;
    (Promise.prototype as any).then=function(this:Promise<unknown>,fulfilled:any,rejected?:any):Promise<unknown>{
        overrides++;console.log('prototype then override',arguments.length);
        return savedThen.call(this,fulfilled,rejected);
    };
    const result=Promise.resolve(8).then(value=>value+1);
    Promise.prototype.then=savedThen;
    console.log('prototype then calls',overrides);
    result.then(value=>console.log('prototype then result',value));
    const borrowed={then:Promise.prototype.then};
    try{borrowed.then(()=>0);}catch(error){console.log('borrowed then brand',(error as Error).name);}
}
for (const count of [0, 1, 2, 7]) {
    let finish!: (value: number) => void;
    const pending = new Promise<number>(resolve => {finish = resolve;});
    for (let i = 0; i < count; i++) pending.then(value => {
        console.log('pending fan-out', count, i, value);
        if (i === 0) pending.then(next => console.log('reentrant handler', count, next));
    });
    finish(count);
    finish(99);
    pending.then(value => console.log('late handler', count, value));
}
{
    let fail!: (reason: string) => void;
    const pending = new Promise<number>((resolve, reject) => {fail = reject;});
    for (let i = 0; i < 7; i++) pending.catch(error => console.log('rejected fan-out', i, error));
    fail('reason');
    pending.catch(error => console.log('late rejection handler', error));
}
Promise.resolve(2).then((value, other = 7) => value + other).then(value => console.log('numeric handler default second', value));
Promise.resolve(undefined as unknown as number).then((value = 5) => value + 1).then(value => console.log('numeric handler default first', value));
Promise.resolve(3).then(() => 12).then(value => console.log('numeric handler ignores number', value));
Promise.resolve('text').then(() => 13).then(value => console.log('numeric handler ignores text', value));
Promise.resolve(null).then(() => 14).then(value => console.log('numeric handler ignores null', value));
Promise.resolve({value: 4}).then(() => 15).then(value => console.log('numeric handler ignores object', value));
Promise.reject<number>('numeric rejection').then(value => value + 1).catch(error => console.log('numeric handler forwards rejection', error));
Promise.resolve(1).then((value): number => {
    if (value > 0) throw new Error('numeric callback failed');
    return value;
}).catch(error => console.log('numeric handler exception', (error as Error).message));
Promise.resolve(4).then(() => undefined as unknown as number).then(value => value + 1).then(value => console.log('numeric handler undefined result', Number.isNaN(value)));
Promise.resolve(3).then(function(value): number {return value + arguments.length;}).then(value => console.log('handler sees arguments', value));
Promise.resolve(3).then(function(this: unknown, value): number {return this === undefined ? value : 100;}).then(value => console.log('handler sees receiver', value));
{
    let unlock!: (value: number) => void;
    const nested = new Promise<number>(resolve => {unlock = resolve;});
    const returned: any = {then(resolve: any, reject: any): void {
        resolve(nested);
        reject('ignored while adopting');
        resolve(99);
        queueMicrotask(() => unlock(23));
    }};
    Promise.resolve(0).then(() => returned).then(value => console.log('returned thenable resolves once', value));
}
{
    let finish!: (value: number) => void;
    const pending = new Promise<number>(resolve => {finish = resolve;});
    pending.then(value => console.log('mixed reaction first', value));
    Promise.all([pending]).then(values => console.log('mixed reaction all', values[0]));
    async function observe(): Promise<void> {
        const value = await pending;
        console.log('mixed reaction await', value);
    }
    observe();
    pending.then(value => console.log('mixed reaction last', value));
    finish(6);
}

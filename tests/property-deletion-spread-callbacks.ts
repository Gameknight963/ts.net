export {};
function remove(target: {a?: number}, operation: typeof Reflect.deleteProperty): boolean {return operation(target,'a');}
const first = {a: 1};console.log('literal', remove(...[first,Reflect.deleteProperty] as const), first.a);
function namespace(target: {b?: number}, operations: typeof Reflect): boolean {return operations.deleteProperty(target,'b');}
const second = {b: 2};console.log('namespace', namespace(...[second,Reflect] as const), second.b);
function nested(target: {c?: number}, operation: typeof Reflect.deleteProperty): boolean {return operation(target,'c');}
const third = {c: 3};console.log('nested', nested(...[...[third] as const,...[Reflect.deleteProperty] as const] as const), third.c);
function forwarded(target: {d?: number}, operation: typeof Reflect.deleteProperty): boolean {return operation(target,'d');}
function forward(target: {d?: number}, operation: typeof Reflect.deleteProperty): boolean {return forwarded(...[target,operation] as const);}
const fourth = {d: 4};console.log('forward', forward(fourth,Reflect.deleteProperty), fourth.d);

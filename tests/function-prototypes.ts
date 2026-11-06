function declaration(value:number):number{return value;}
const expression=function named(value:number):number{return value;};
const arrow=(value:number)=>value;
const methods={method():void {}};
console.log(Object.hasOwn(declaration,'prototype'),Object.hasOwn(expression,'prototype'),Object.hasOwn(arrow,'prototype'),Object.hasOwn(methods.method,'prototype'));
console.log(Object.hasOwn(declaration.bind(null),'prototype'),declaration.length,expression.name);
const dynamic:any=declaration;
const initial=dynamic.prototype;
console.log(initial===dynamic.prototype,initial.constructor===declaration,Object.hasOwn(initial,'constructor'),Object.keys(initial).length);
initial.extra=1;console.log(Object.keys(initial).length,Object.values(initial)[0]);
dynamic.prototype=null;console.log(dynamic.prototype===null,Object.hasOwn(dynamic,'prototype'));
dynamic.prototype=undefined;console.log(dynamic.prototype===undefined,Object.hasOwn(dynamic,'prototype'));
dynamic.prototype=3;console.log(dynamic.prototype,Object.hasOwn(dynamic,'prototype'));
function create():any {return function local():void {};}
const first=create(),second=create();
console.log(first!==second,first.prototype!==second.prototype,first.prototype.constructor===first,second.prototype.constructor===second);
function localDeclaration():any {function local():void {}return local;}
const local=localDeclaration();console.log(Object.hasOwn(local,'prototype'),local.prototype.constructor===local);

for(const constructor of [Object,Number,Error] as any[]) {console.log(Object.hasOwn(constructor,'prototype'));try{constructor.prototype=null;}catch(error){console.log(error instanceof TypeError);}}

for(const value of [undefined,null,1,"text",true,Symbol("x"),1n,{},[],()=>{},new Date(0),new Map(),new Uint8Array(1)])console.log(Object.isExtensible(value),Object.isSealed(value),Object.isFrozen(value));
function tagged(parts:TemplateStringsArray,...values:unknown[]){console.log("template",Object.isExtensible(parts),Object.isSealed(parts),Object.isFrozen(parts),Object.isFrozen(parts.raw),Reflect.isExtensible(parts));}
tagged`first${1}second`;
const raw=(JSON as any).rawJSON("1");console.log("raw",Object.isExtensible(raw),Object.isSealed(raw),Object.isFrozen(raw),Reflect.isExtensible(raw));
for(const value of [null,undefined,1,"x",Symbol("x")])try{Reflect.isExtensible(value as any);}catch(error){console.log("reflect-primitive",(error as Error).name);}
const mutable={value:1};console.log("mutable",Reflect.isExtensible(mutable));(mutable as any).extra=2;console.log(Object.keys(mutable).join("|"));
for(const method of [Object.isExtensible,Object.isSealed,Object.isFrozen,Reflect.isExtensible])console.log("metadata",method.name,method.length);

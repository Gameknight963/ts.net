export {};
class Default extends Promise<number>{}
class Base extends Promise<number>{static get [Symbol.species]():PromiseConstructor{return Promise;}}
class Other extends Promise<number>{}
class Custom extends Promise<number>{static get [Symbol.species]():any{return Other;}}
class NullSpecies extends Promise<number>{static get [Symbol.species]():any{return null;}}
async function main():Promise<void>{
 console.log("getter",Promise[Symbol.species]===Promise,Default[Symbol.species]===Default);
 for(const C of [Default,Base,Custom,NullSpecies]){
  const source=new C(resolve=>resolve(3));
  const then=source.then(value=>value+1);const caught=source.catch(()=>0);const final=source.finally(()=>{});
  console.log("types",C.name,then instanceof Default,then instanceof Base,then instanceof Other,then.constructor===Promise,caught.constructor===then.constructor,final.constructor===then.constructor);
  console.log("values",await then,await caught,await final);
 }
 const source=Promise.resolve(5);(source as any).constructor={[Symbol.species]:Other};const result=source.then(value=>value*2);console.log("record",result instanceof Other,await result);
 for(const species of [undefined,null,1,{},()=>{}]){
  const promise=Promise.resolve(1);(promise as any).constructor={[Symbol.species]:species};
  try{const next=promise.then(value=>value+1);console.log("fallback",next.constructor===Promise,await next);}catch(error){console.log("invalid",(error as Error).name);}
 }
 class Box{value:any;constructor(executor:any){executor((value:any)=>{this.value=value;},()=>{});}}
 const custom=Promise.resolve(8);(custom as any).constructor={[Symbol.species]:Box};const box=custom.then(value=>value+1) as any;await Promise.resolve();console.log("box",box instanceof Box,box.value);
 const throwing=Promise.resolve(1);(throwing as any).constructor={get [Symbol.species](){throw "species";}};
 try{throwing.then(()=>0);}catch(error){console.log("getter throws",error);}
 const descriptor=Object.getOwnPropertyDescriptor(Promise,Symbol.species)!;console.log("metadata",descriptor.enumerable,descriptor.configurable,descriptor.set,descriptor.get!.name,descriptor.get!.length);
}
main().catch(error=>console.log("failure",String(error)));

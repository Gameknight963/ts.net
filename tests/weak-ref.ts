const object={value:7};const reference=new WeakRef(object);console.log(reference.deref()===object,reference.deref()?.value,reference instanceof WeakRef,Object.keys(reference).length,Object.prototype.toString.call(reference));
const array=[1,2];console.log(new WeakRef(array).deref()===array);const fn=()=>3;console.log(new WeakRef(fn).deref()===fn);
for(const value of [null,undefined,1,true,"x"])try{new WeakRef(value as any);}catch(error){console.log("target",(error as Error).name);}
try{(WeakRef as any)(object);}catch(error){console.log("new",(error as Error).name);}
try{WeakRef.prototype.deref.call({});}catch(error){console.log("receiver",(error as Error).name);}
class Reference extends WeakRef<object>{marker=9;}const derived=new Reference(object);console.log("derived",derived instanceof Reference,derived instanceof WeakRef,derived.deref()===object,derived.marker);
console.log("metadata",WeakRef.name,WeakRef.length,WeakRef.prototype.deref.name,WeakRef.prototype.deref.length);
Promise.resolve().then(()=>console.log("job",reference.deref()===object));

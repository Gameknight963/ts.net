export {};
for(const Base of [Symbol,BigInt,ReadableStreamDefaultController,ReadableByteStreamController,ReadableStreamBYOBRequest] as any[]){
    try {
        let fields=0;
        class Derived extends Base {field=++fields;}
        console.log("class",Base.name,Object.getPrototypeOf(Derived)===Base,Object.getPrototypeOf(Derived.prototype)===Base.prototype);
        try {new Derived();console.log("constructed");} catch(error){console.log("new",(error as Error).name,fields);}
        class Replacement extends Base {constructor(replace:boolean){if(replace)return {replacement:true} as any;super();}}
        const replacement:any=new Replacement(true);
        console.log("replacement",replacement.replacement,replacement instanceof Replacement);
        const reflected=Reflect.construct(Object,[],Base);
        console.log("new-target",Object.getPrototypeOf(reflected)===Base.prototype);
    } catch(error){console.log("early",Base.name,(error as Error).name);}
}
const target=(function Target(){}).bind(null);
let prototypes=0;
Object.defineProperty(target,"prototype",{get(){prototypes++;return {};}});
for(const Base of [Symbol,BigInt,ReadableStreamDefaultController,ReadableByteStreamController,ReadableStreamBYOBRequest]){
    try {Reflect.construct(Base,[],target);} catch(error){console.log("throw-before-prototype",Base.name,(error as Error).name,prototypes);}
}
const throwing=(function Target(){}).bind(null);
Object.defineProperty(throwing,"prototype",{get(){throw "prototype";}});
for(const Base of [Symbol,BigInt,ReadableStreamDefaultController,ReadableByteStreamController,ReadableStreamBYOBRequest]){
    try {Reflect.construct(Base,[],throwing);} catch(error){console.log("prototype-error",Base.name,error instanceof Error?error.name:error);}
}

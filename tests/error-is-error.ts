const isError:any=(Error as any).isError;
class Failure extends Error {code=1;}
for(const value of [new Error(),new TypeError(),new RangeError(),new SyntaxError(),new URIError(),new EvalError(),new ReferenceError(),new AggregateError([]),new Failure(),{},[],null,undefined,1,"Error",Error.prototype,Object.create(Error.prototype),{name:"Error",message:"bad"}])console.log(isError(value));
const detached=new Error("detached");Object.setPrototypeOf(detached,null);console.log("detached",isError(detached),detached instanceof Error);
const fake=Object.create(Error.prototype);console.log("fake",isError(fake),fake instanceof Error);
console.log("borrowed",isError.call(null,new Error()),isError(),isError.name,isError.length);
console.log("own",Object.hasOwn(Error,"isError"),Object.hasOwn(TypeError,"isError"),(TypeError as any).isError===isError,Object.keys(Error).includes("isError"));
const descriptor=Object.getOwnPropertyDescriptor(Error,"isError")!;console.log("descriptor",descriptor.writable,descriptor.enumerable,descriptor.configurable);
try{(null as any).toString();}catch(error){console.log("runtime",isError(error));}
try{throw 1;}catch(error){console.log("thrown",isError(error));}

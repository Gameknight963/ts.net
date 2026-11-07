export {};
for(const name of [undefined,"Error","NotFoundError","InvalidCharacterError","AbortError","SyntaxError","DataCloneError","custom"]){const error=new DOMException("message",name);console.log("error",error.name,error.message,error.code,error instanceof DOMException,error instanceof Error,String(error),Object.prototype.toString.call(error));}
const empty=new DOMException();console.log("default",empty.name,empty.message,empty.code);
class Custom extends DOMException{extra=7;}const custom=new Custom("custom","AbortError");console.log("subclass",custom instanceof Custom,custom instanceof DOMException,custom instanceof Error,custom.extra,custom.name,custom.code);
console.log("constants",DOMException.INDEX_SIZE_ERR,DOMException.ABORT_ERR,DOMException.DATA_CLONE_ERR,DOMException.prototype.INVALID_CHARACTER_ERR);
for(const fn of [()=>atob("!"),()=>btoa("\u0100")])try{fn();}catch(error){const e=error as DOMException;console.log("base64",e instanceof DOMException,e instanceof Error,e.name,e.code);}
const descriptor=Object.getOwnPropertyDescriptor(DOMException,"ABORT_ERR")!;console.log("metadata",descriptor.value,descriptor.enumerable,descriptor.configurable,descriptor.writable);

for(const name of ["IndexSizeError","DOMStringSizeError","HierarchyRequestError","WrongDocumentError","NoDataAllowedError","NoModificationAllowedError","NotSupportedError","InUseAttributeError","InvalidStateError","InvalidModificationError","NamespaceError","InvalidAccessError","ValidationError","TypeMismatchError","SecurityError","NetworkError","URLMismatchError","QuotaExceededError","TimeoutError","InvalidNodeTypeError"])console.log("code",name,new DOMException("",name).code);
for(const key of ["name","message","code"]){const d=Object.getOwnPropertyDescriptor(DOMException.prototype,key)!;console.log("accessor",key,d.enumerable,d.configurable,d.get!.name,d.get!.length,d.set);}
console.log("coerce",new DOMException(123 as any,456 as any).message,new DOMException(123 as any,456 as any).name);
const error=new DOMException("message","NotFoundError");try{throw error;}catch(caught){console.log("catch",caught===error,(caught as Error).stack!.includes("NotFoundError: message"));}

console.log("isError",(Error as any).isError(error));

const own=new DOMException("own","Error");console.log("own keys",Object.keys(own).join("|"),Object.getOwnPropertyNames(own).join("|"));
for(const key of ["name","message","code"]){try{(own as any)[key]="changed";}catch(error){console.log("readonly",key,(error as Error).name);}}
console.log("prototype keys",Object.keys(DOMException.prototype).join("|"));
const getCode=Object.getOwnPropertyDescriptor(DOMException.prototype,"code")!.get!;
try{getCode.call({});}catch(error){console.log("brand",(error as Error).name);}

export {};
interface Awaitable<T> extends PromiseLike<T> { label?: string; }
interface Nested<T> extends Awaitable<T> {}
interface FullPromise<T> extends Promise<T> {}
type ReadonlyPromise<T> = Readonly<Promise<T>>;
type ThenOnly<T> = Pick<Promise<T>, "then">;
type Tagged<T> = PromiseLike<T> & { label?: string };
async function read(value: Awaitable<number>): Promise<number> {return await value;}
function forward<T>(value: Nested<T>): Nested<T> {return value;}
async function readonly(value:ReadonlyPromise<number>):Promise<number>{return await value;}
async function picked(value:ThenOnly<number>):Promise<number>{return await value;}
async function tagged(value:Tagged<number>):Promise<number>{return await value;}
async function full(value:FullPromise<number>):Promise<number>{return value.then(x=>x+1).finally(()=>{});}
class Thenable<T> implements PromiseLike<T> {
    constructor(public value:T) {}
    then<A=T,B=never>(fulfilled?:((value:T)=>A|PromiseLike<A>)|null,rejected?:((reason:any)=>B|PromiseLike<B>)|null):PromiseLike<A|B> {
        return Promise.resolve(this.value).then(fulfilled,rejected);
    }
}
async function main():Promise<void>{
    const promise=Promise.resolve(7);
    console.log("native",await read(promise),await readonly(promise),await picked(promise),await tagged(promise),await full(promise));
    console.log("identity",forward(promise)===promise,await forward(Promise.resolve("text")));
    const custom=new Thenable(9);
    console.log("class",await read(custom),forward(custom)===custom,custom.value);
    const source=Promise.resolve(11);
    const plain:Awaitable<number>={then:source.then.bind(source),label:"plain"};
    console.log("plain",await read(plain),forward(plain)===plain,forward(plain).label);
    const rejected:Awaitable<number>=Promise.reject(new Error("rejected"));
    try{await read(rejected);}catch(error){console.log("rejection",(error as Error).message);}
    const nested:Awaitable<number>=Promise.resolve(Promise.resolve(13));
    console.log("nested",await read(nested),(await Promise.all([forward(promise),forward(plain)])).join(","));
    function partial(value:Partial<PromiseLike<number>>):Partial<PromiseLike<number>> {return value;}
    console.log("partial",partial(promise)===promise,Object.keys(partial({})).length);
    const absent:Partial<PromiseLike<number>>={then:undefined};
    console.log("absent",Object.hasOwn(absent,"then"),absent.then);
    const throwing:Awaitable<number>={get then():PromiseLike<number>["then"]{throw new Error("getter");}};
    try{await read(throwing);}catch(error){console.log("getter",(error as Error).message);}
    interface Detailed<T> extends Awaitable<T> { count:number; }
    const detailed:Detailed<number>={then:source.then.bind(source),count:4};
    function detail(value:Detailed<number>):number{return value.count;}
    console.log("extra fields",detail(detailed),await read(detailed));
    const immutable:Readonly<Awaitable<number>>=source;
    console.log("readonly awaitable",await read(immutable));
    interface Data {then:number; value:number;}
    function data(input:Data):number{return input.then+input.value;}
    console.log("data",data({then:2,value:3}));
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});

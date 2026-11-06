let events='';
function mark(value:string):void {events+=value;}
async function defaults():Promise<number> {
    const source:(number|undefined)[]=[undefined,2,undefined,4];
    const [first=await Promise.resolve(1),second,,...rest]=source;
    const {x=await 3,y=await (mark('unused'),99)}={x:undefined,y:5};
    return first+second!+rest[0]!+x+y;
}
async function order():Promise<string> {
    const source={get a():undefined {mark('a');return undefined;},get b():number {mark('b');return 7;},c:8};
    const {[await (mark('k'),'a')]:first=await (mark('d'),6),b,...rest}=source;
    return ''+first+b+rest.c;
}
async function liveIterator():Promise<number> {
    const source:(number|undefined)[]=[undefined,2,3];
    const [first=await (source[1]=9,1),second,...rest]=source;
    return first+second!+rest[0]!;
}
async function exhausted():Promise<string> {
    const source:number[]=[];
    const [first=await (source.push(9),1),second=await 2,...rest]=source;
    return ''+first+second+rest.length;
}
async function nested():Promise<number> {
    const {a:[first=await 3,...rest]=await [undefined,4],b=await 5}={} as {a?:number[],b?:number};
    return first+rest[0]!+b;
}
async function earlyBindings():Promise<number> {
    const readers:(()=>number)[]=[];
    const [first=await 2,second=await (readers.push(()=>first),3)]=[] as number[];
    await 0;
    return readers[0]()+second;
}
async function rejected():Promise<void> {
    const [value=await Promise.reject('default rejected')]=[];
    console.log('unreachable',value);
}
async function nullSource():Promise<void> {
    const {[await (mark('bad-key'),'x')]:value}=null as any;
    console.log('unreachable',value);
}
defaults().then(value=>console.log('defaults',value));
order().then(value=>console.log('order',value,events));
liveIterator().then(value=>console.log('live',value));
exhausted().then(value=>console.log('exhausted',value));
nested().then(value=>console.log('nested',value));
earlyBindings().then(value=>console.log('early bindings',value));
rejected().catch(error=>console.log(error));
nullSource().catch(error=>console.log('null source',error.name));
async function varBindings():Promise<string> {
    const read=()=>value;
    const before=typeof read();
    var [value=await 9]=[] as number[];
    return before+read();
}
async function unicode():Promise<string> {
    const [first=await 'unused',second,...rest]='😀ab';
    return first+second+rest[0];
}
async function coerceKey():Promise<string> {
    let log='';
    const key=1;
    const source={get 1():undefined {log+='g';return undefined;},y:8};
    const {[await (log+='k',Promise.resolve<1>(key))]:value=await (log+='d',4),...rest}=source;
    return log+value+rest.y+Object.keys(rest).length;
}
async function falsyDefaults():Promise<string> {
    const [first=await 9,second=await 8,third=await 7]=[null,0,false] as any[];
    return ''+first+second+third;
}
async function tdz():Promise<number> {
    const read=()=>later;
    queueMicrotask(()=>{try{read();}catch(error){console.log('pattern tdz',(error as Error).name);}});
    const [first=await 1,later=await 2]=[] as number[];
    return first+read();
}
varBindings().then(value=>console.log('var bindings',value));
unicode().then(value=>console.log('unicode',value));
coerceKey().then(value=>console.log('coercion',value));
falsyDefaults().then(value=>console.log('falsy',value));
tdz().then(value=>console.log('tdz result',value));

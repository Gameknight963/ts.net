async function values():Promise<number> {
    let total=0;
    const thenable={then(resolve:(value:number)=>void):void{queueMicrotask(()=>resolve(3));}};
    for await(const value of [Promise.resolve(1),2,thenable])total+=value;
    return total;
}
values().then(value=>console.log('values',value));
async function captures():Promise<number> {
    const readers:(()=>number)[]=[];
    for await(let value of [1,Promise.resolve(2),3]){readers.push(()=>value);value+=10;}
    return readers[0]()+readers[1]()+readers[2]();
}
captures().then(value=>console.log('captures',value));
async function patterns():Promise<number> {
    let total=0;
    for await(const [first=await 2,...rest] of [Promise.resolve([undefined,3]),[4,5]])total+=first+rest[0]!;
    return total;
}
patterns().then(value=>console.log('patterns',value));
async function live():Promise<string> {
    const source:(number|undefined)[]=[1,,3];
    let result='';
    for await(const value of source){result+=String(value);if(value===1)source.push(4);}
    return result;
}
live().then(value=>console.log('live',value));
async function unicode():Promise<string> {let result='';for await(const value of '\uD83D\uDE00ab')result+=value.length;return result;}
unicode().then(value=>console.log('unicode',value));
async function args(first:number,second:Promise<number>):Promise<number> {let total=0;for await(const value of arguments)total+=value;return total;}
args(2,Promise.resolve(3)).then(value=>console.log('arguments',value));
const rejection={value:7};
async function rejected():Promise<boolean> {try{for await(const value of [Promise.reject(rejection)])console.log('wrong',value);}catch(error){return error===rejection;}return false;}
rejected().then(value=>console.log('rejected',value));
async function jumps():Promise<string> {
    let log='';
    outer: for await(const first of [1,2,3]) {
        for await(const second of [1,2,3]) {
            try {log+=''+first+second;if(second===1)continue;if(first===2)break outer;}
            finally {await 0;log+='f';}
        }
    }
    return log;
}
jumps().then(value=>console.log('jumps',value));
async function target():Promise<number> {const output={value:0};for await(output[await Promise.resolve<'value'>('value')] of [Promise.resolve(4)]){}return output.value;}
target().then(value=>console.log('target',value));
async function returnValue():Promise<number> {for await(const value of [1,2])try{return value;}finally{await 0;console.log('body finally');}return 0;}
returnValue().then(value=>console.log('returned',value));
async function empty():Promise<string> {for await(var value of [] as number[])console.log('wrong');return typeof value!;}
empty().then(value=>console.log('empty',value));
async function invalid():Promise<void> {try{for await(const value of null as any)console.log(value);}catch(error){console.log('invalid',(error as Error).name);}}
invalid();
async function bindingError():Promise<void> {try{for await(const [value] of [null as any])console.log(value);}catch(error){console.log('binding error',(error as Error).name);}}
bindingError();

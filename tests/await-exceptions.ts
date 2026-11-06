async function caught():Promise<string> {
    try {await Promise.reject('rejected');return 'wrong';}
    catch(error) {return 'caught '+await error;}
    finally {await 0;console.log('caught finally');}
}
caught().then(value=>console.log(value));
async function returned():Promise<number> {
    let value=1;
    try {await 0;return value;}
    finally {value=await 2;console.log('return finally',value);}
}
returned().then(value=>console.log('saved return',value));
async function overridden():Promise<number> {try{return await 1;}finally{return await 3;}}
overridden().then(value=>console.log('override return',value));
async function overrideThrow():Promise<void> {try{await Promise.reject('old');}finally{await 0;throw 'new';}}
overrideThrow().catch(error=>console.log('override throw',error));
async function nested():Promise<string> {
    let log='';
    try {
        try {await 0;throw 'x';}
        catch(error) {log+=await error;throw 'y';}
        finally {log+='i';await 0;log+='I';}
    } catch(error) {log+=await error;}
    finally {log+='o';await 0;log+='O';}
    return log;
}
nested().then(value=>console.log('nested',value));
async function jumps():Promise<string> {
    let log='';
    outer: for(let i=0;i<4;i++) {
        try {
            await 0;
            if(i===0)continue;
            if(i===2)break outer;
            log+=i;
        } finally {log+='f';await 0;log+=i;}
    }
    return log+'end';
}
jumps().then(value=>console.log('jumps',value));
async function insideTry():Promise<string> {
    let log='';
    try {
        for(let i=0;i<3;i++) {await 0;if(i===1)continue;log+=i;}
        switch(await 1){case 1:log+='s';break;}
        label: {await 0;log+='l';break label;}
        log+='t';
    } finally {log+='f';await 0;log+='F';}
    return log;
}
insideTry().then(value=>console.log('internal transfers',value));
async function finalizerJump():Promise<string> {
    let log='';
    for(let i=0;i<3;i++) {
        try {await 0;return 'wrong';}
        finally {await 0;log+=i;continue;}
    }
    return log;
}
finalizerJump().then(value=>console.log('finally continue',value));
async function finalizerLoop():Promise<string> {
    try {throw 'saved';}
    finally {for(let i=0;i<2;i++){await 0;console.log('finally loop',i);}}
}
finalizerLoop().catch(error=>console.log('preserved throw',error));
const rejection={value:4};
async function identity():Promise<boolean> {
    try {return await Promise.reject(rejection);}
    catch(error) {await 0;return error===rejection;}
}
identity().then(value=>console.log('identity',value));
async function returnPromise():Promise<number> {
    try {return Promise.reject('returned promise');}
    catch(error) {console.log('wrong catch',error);return 0;}
    finally {await 0;console.log('promise finally');}
}
returnPromise().catch(error=>console.log('promise rejection',error));
async function catchPattern():Promise<number> {
    try {await 0;throw {first:4,second:5};}
    catch({first,second}:any) {const read=()=>first;await 0;return read()+second;}
}
catchPattern().then(value=>console.log('catch pattern',value));
async function noBinding():Promise<number> {try{throw await 1;}catch{await 0;}return 7;}
noBinding().then(value=>console.log('no binding',value));
async function syncError():Promise<string> {try{await 0;return (null as any).value;}catch(error){await 0;return (error as Error).name;}}
syncError().then(value=>console.log('runtime error',value));
async function finalizerError():Promise<void> {
    try {try{return;}finally{await Promise.reject('finally rejected');}}
    catch(error) {console.log('outer catches',error);}
}
finalizerError().then(()=>console.log('finalizer error done'));
async function longRun():Promise<number> {
    let total=0;
    for(let i=0;i<1000;i++){try{if(i===999)await 0;total++;}finally{total++;}}
    return total;
}
longRun().then(value=>console.log('long try loop',value));
async function catchBindingError():Promise<string> {
    try {try{await 0;throw null;}catch({value}:any){return value;}}
    catch(error) {await 0;return (error as Error).name;}
}
catchBindingError().then(value=>console.log('catch binding error',value));
async function finalizerBreak():Promise<string> {
    let log='';
    exit: {
        try {await 0;throw 'discarded';}
        finally {log+='f';await 0;break exit;}
    }
    return log+'done';
}
finalizerBreak().then(value=>console.log('finally break',value));
async function catchClosures():Promise<string> {
    const readers:(()=>number)[]=[];
    for(let i=0;i<3;i++) {
        try {throw await i;}
        catch(error) {let value=error as number;readers.push(()=>value);await 0;value+=10;}
    }
    return ''+readers[0]()+readers[1]()+readers[2]();
}
catchClosures().then(value=>console.log('catch closures',value));
class Receiver {
    value=5;
    async method(amount:number):Promise<number> {
        try {await 0;throw amount;}
        catch(error) {return this.value+await (error as number)+arguments[0];}
        finally {await 0;this.value++;}
    }
}
const receiver=new Receiver();
receiver.method(3).then(value=>console.log('receiver',value,receiver.value));
async function internalFinallyBreak():Promise<number> {
    try {return 9;}
    finally {
        local: {await 0;break local;}
        try {await 0;throw 'local';}catch(error){console.log('finally nested catch',error);}
    }
}
internalFinallyBreak().then(value=>console.log('finally keeps return',value));
async function throwUndefined():Promise<string> {try{throw await undefined;}catch(error){await 0;return typeof error;}}
throwUndefined().then(value=>console.log('undefined rejection',value));

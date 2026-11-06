async function choose(value:number):Promise<string> {
    let result='';
    switch(await value) {
        case await 1:result+='one';await 0;
        case await 2:result+='two';break;
        default:result+='default';await 0;
        case await 3:result+='three';
    }
    return result+'!';
}
for(let i=1;i<5;i++)choose(i).then(value=>console.log('choose',value));
async function searchOrder():Promise<string> {
    let log='';
    switch(await (log+='s',4 as number)) {
        case await (log+='a',1):log+='A';break;
        default:log+='D';
        case await (log+='b',2):log+='B';break;
        case await (log+='c',3):log+='C';
    }
    return log;
}
searchOrder().then(value=>console.log('search',value));
async function scope(value:number):Promise<number> {
    switch(await value) {
        case 1:
            let first=await 4;
            const read=()=>first;
            await 0;
            first++;
            return read();
        case 2:
            return later();
        default:
            const other=await 8;
            return other;
        case 3:
            function later():number {return 6;}
            return later();
    }
}
scope(1).then(value=>console.log('scope',value));
scope(2).then(value=>console.log('hoisted',value));
async function finalizer(override:boolean):Promise<string> {
    let log='';
    switch(await (1 as number)) {
        case 1:
            try {log+='t';break;}
            finally {log+='f';if(override)return log+'override';}
        default:log+='wrong';
    }
    log+='a';
    return log;
}
finalizer(false).then(value=>console.log('finally break',value));
finalizer(true).then(value=>console.log('finally return',value));
async function nested():Promise<number> {
    let result=0;
    switch(await (1 as number)) {
        case 1:
            for(let i=0;i<4;i++){if(i===1)continue;if(i===3)break;result+=i;}
            switch(await 2){case 2:result+=await 3;break;default:result=-1;}
            result+=await 4;break;
        default:result=-2;
    }
    return result;
}
nested().then(value=>console.log('nested',value));
async function noMatch():Promise<number> {switch(await (1 as number)){case await 2:throw 'wrong';}return 7;}
noMatch().then(value=>console.log('no match',value));
async function empty():Promise<number> {switch(await (1 as number)){}return 8;}
empty().then(value=>console.log('empty',value));
async function rejectCase():Promise<void> {switch(1){case await Promise.reject('case rejected'):console.log('wrong');}}
rejectCase().catch(error=>console.log(error));
async function tdz():Promise<void> {
    switch(await (1 as number)) {
        case 1:await 0;console.log(read());break;
        case 2:let value=3;function read():number{return value;}break;
    }
}
tdz().catch(error=>console.log('switch tdz',error.name));
async function afterFinally():Promise<void> {
    switch(await (1 as number)){case 1:try{break;}catch(error){console.log('wrong catch');}finally{console.log('before rejection');}}
    throw 'after switch';
}
afterFinally().catch(error=>console.log(error));
async function labeled():Promise<string> {
    let log='';
    exit: {
        log+='a';
        await 0;
        switch(await 1) {
            case 1:
                try {log+='b';break exit;}
                finally {log+='f';}
        }
        log+='wrong';
    }
    return log+'c';
}
labeled().then(value=>console.log('labeled',value));
async function labeledBlock():Promise<number> {
    let result=0;
    exit: {
        for(let i=0;i<4;i++){if(i===2)break exit;result+=i;}
        await 0;
        result=-1;
    }
    return result+await 2;
}
labeledBlock().then(value=>console.log('labeled block',value));

async function whiles():Promise<number> {
    let i=0,total=0;
    while(await (i<6)) {
        i++;
        if(i===2)continue;
        total+=await i;
        if(i===4)break;
    }
    do {total+=await 2;} while(await false);
    return total;
}
whiles().then(value=>console.log('while do',value));
async function fors():Promise<string> {
    const readers:(()=>number)[]=[];
    let updates=0,total=0;
    for(let i=await 0;await (i<5);i+=await (updates++,1)) {
        readers.push(()=>i);
        if(i===1)continue;
        total+=await i;
        if(i===3)break;
    }
    return ''+total+updates+readers[0]()+readers[1]()+readers[2]()+readers[3]();
}
fors().then(value=>console.log('for captures',value));
async function initializerCapture():Promise<string> {
    let initial:()=>number=()=>-1;
    const tests:(()=>number)[]=[],updates:(()=>number)[]=[],bodies:(()=>number)[]=[];
    for(let i=await 0,read=(initial=()=>i);(tests.push(()=>i),i<3);(updates.push(()=>i),i++)) {
        bodies.push(()=>i);
        await 0;
    }
    return ''+initial()+tests[0]()+tests[1]()+tests[2]()+tests[3]()+updates[0]()+updates[1]()+updates[2]()+bodies[0]()+bodies[1]()+bodies[2]();
}
initializerCapture().then(value=>console.log('iteration frames',value));
async function finalizers():Promise<string> {
    let log='';
    for(let i=0;await (i<4);i++) {
        try {log+=i;if(i===0)continue;break;}
        finally {log+='f';i++;}
    }
    return log+'done';
}
finalizers().then(value=>console.log('for finally',value));
async function nested():Promise<number> {
    let total=0;
    outer: for(let i=0;await(i<4);i++) {
        let j=0;
        do {
            j++;
            if(j===2)continue outer;
            switch(await j) {case 1:total+=await i;break;}
        } while(await(j<3));
    }
    return total;
}
nested().then(value=>console.log('nested loops',value));
async function varLoop():Promise<string> {
    const readers:(()=>number)[]=[];
    for(var i=0;await (i<3);i++)readers.push(()=>i);
    return ''+i+readers[0]()+readers[1]()+readers[2]();
}
varLoop().then(value=>console.log('var loop',value));
async function noHeader():Promise<number> {let i=0;for(;;){await 0;if(++i===3)return i;}}
noHeader().then(value=>console.log('no header',value));
async function synchronousRun():Promise<number> {
    let i=0;
    while(i<50000){if(i===49999)await 0;i++;}
    return i;
}
synchronousRun().then(value=>console.log('long run',value));
async function bodyCaptures():Promise<number> {
    const readers:(()=>number)[]=[];
    let i=0;
    while(i<3){let value=i++;readers.push(()=>value);await 0;value+=10;}
    return readers[0]()+readers[1]()+readers[2]();
}
bodyCaptures().then(value=>console.log('body captures',value));
async function patternLoop():Promise<number> {
    const readers:(()=>number)[]=[];
    for(let [i=await 0,j=await 4]=[] as number[];await(i<j);i++)readers.push(()=>i+j);
    return readers[0]()+readers[1]()+readers[2]()+readers[3]();
}
patternLoop().then(value=>console.log('pattern loop',value));
async function rejected():Promise<void> {while(await Promise.reject('condition rejected'))console.log('wrong');}
rejected().catch(error=>console.log(error));
async function forOf():Promise<string> {
    const source:(number|undefined)[]=[1,undefined,3];
    const readers:(()=>number|undefined)[]=[];
    let log='';
    for(const value of await source) {
        readers.push(()=>value);
        await 0;
        log+=String(value);
        if(value===1)source.push(4);
        if(value===3)continue;
    }
    return log+':'+readers[0]()+readers[1]()+readers[2]()+readers[3]();
}
forOf().then(value=>console.log('for of',value));
async function forOfPatterns():Promise<number> {
    const readers:(()=>number)[]=[];
    for(const [first=await 2,...rest] of [[undefined,3],[4,5]]) {
        readers.push(()=>first+rest[0]!);
        await 0;
    }
    return readers[0]()+readers[1]();
}
forOfPatterns().then(value=>console.log('of patterns',value));
async function forIn():Promise<string> {
    const source=[1,2,3];
    const readers:(()=>string)[]=[];
    let log='';
    for(const key in await source) {
        readers.push(()=>key);
        log+=await key;
        if(key==='0')source.pop();
    }
    return log+readers[0]()+readers[1]();
}
forIn().then(value=>console.log('for in',value));
async function assignmentTarget():Promise<number> {
    const target={value:0};
    let total=0;
    for(target[await Promise.resolve<'value'>('value')] of [1,2,3])total+=await target.value;
    return total;
}
assignmentTarget().then(value=>console.log('of target',value));
async function forOfVar():Promise<string> {
    const readers:(()=>number)[]=[];
    for(var value of [1,2,3]){await 0;readers.push(()=>value);}
    return ''+value!+readers[0]()+readers[1]()+readers[2]();
}
forOfVar().then(value=>console.log('of var',value));
async function stringLoop():Promise<string> {let result='';for(const value of '\uD83D\uDE00ab'){await 0;result+=value.length;}return result;}
stringLoop().then(value=>console.log('of string',value));
async function emptyLoop():Promise<string> {for(var value of [] as number[])await 0;for(const key in null as any)await key;return typeof value!;}
emptyLoop().then(value=>console.log('empty loops',value));
async function loopTdz():Promise<void> {
    for(let value of (queueMicrotask(()=>{try{console.log(typeof value);}catch(error){console.log('rhs tdz',(error as Error).name);}}),await Promise.resolve([1])))await 0;
}
loopTdz().catch(error=>console.log('loop tdz',error.name));

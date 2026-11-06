console.log('start');
async function work():Promise<number>{console.log('before');await 1;console.log('after');const value=await 2;console.log('value',value);return value+1;}
work().then(value=>console.log('result',value));console.log('end');
async function branch(flag:boolean):Promise<number>{let total=1;if(await flag){total+=await 2;}else{total+=await 3;}console.log('branch body',flag,total);return total;}
branch(true).then(v=>console.log('branch true',v));branch(false).then(v=>console.log('branch false',v));
async function captured():Promise<number>{const read=()=>value;await 0;let value=4;return read();}
captured().then(v=>console.log('captured',v));
async function variable():Promise<number>{console.log('var before',value);await 0;var value:any=5;return value;}
variable().then(v=>console.log('var after',v));
async function pattern():Promise<number>{const [first,second]=await Promise.resolve([6,7]);await 0;return first+second;}
pattern().then(v=>console.log('pattern',v));
async function rejection():Promise<void>{console.log('reject before');await Promise.reject('failed');console.log('unexpected continuation');}
rejection().catch(e=>console.log('statement rejection',e));
async function throwing():Promise<never>{await 0;throw await Promise.resolve('thrown');}
throwing().catch(e=>console.log('await throw',e));
async function tdz():Promise<number>{const read=()=>late;queueMicrotask(()=>{try{read();}catch(e){console.log('tdz',e instanceof ReferenceError);}});await 0;let late=8;return read();}
tdz().then(v=>console.log('tdz initialized',v));
async function early(flag:boolean):Promise<number>{if(flag){await 0;return 9;}await 0;return 10;}
early(true).then(v=>console.log('early true',v));early(false).then(v=>console.log('early false',v));
async function blocks():Promise<number>{let value=1;{let value=2;await 0;console.log('inner',value);}await 0;return value;}
blocks().then(v=>console.log('outer',v));
class Receiver {value=20;async method(add:number):Promise<number>{await 0;return this.value+arguments[0]+add;}}
new Receiver().method(3).then(v=>console.log('receiver arguments',v));
async function objectPattern():Promise<number>{const {first,second=2,...rest}=await Promise.resolve({first:1,third:3} as {first:number,second?:number,third:number});await 0;return first+second+rest.third;}
objectPattern().then(v=>console.log('object pattern',v));
async function localFunction():Promise<number>{await 0;let value=11;return read();function read():number{return value;}}
localFunction().then(v=>console.log('local function',v));
async function loopsBefore():Promise<number>{const readers:(()=>number)[]=[];for(let i=0;i<3;i++)readers.push(()=>i);await 0;return readers[0]()+readers[1]()+readers[2]();}
loopsBefore().then(v=>console.log('loop closures',v));
async function finalStatement():Promise<void>{await 0;console.log('final statement');}
finalStatement().then(v=>console.log('implicit return',v));
async function assigned():Promise<number>{const copied=Object.assign({},await Promise.resolve({value:14}));await 0;return copied.value;}
assigned().then(v=>console.log('await assign',v),e=>console.log('await assign failed',e.message));
async function arrows():Promise<number>{const value=15;const next=async()=>{await 0;return value+1;};await 0;return await next();}
arrows().then(v=>console.log('nested async arrow',v));
async function declarationList():Promise<number>{const first=await 2,second=await 3;return first+second;}
declarationList().then(v=>console.log('declaration list',v));
async function synchronousTry():Promise<number>{await 0;try{throw 'caught';}catch(e){console.log('sync catch',e);}return 17;}
synchronousTry().then(v=>console.log('sync try after await',v));
async function synchronousJumps():Promise<number> {
    let total=0;
    outer: for(let i=0;i<4;i++) {
        for(let j=0;j<4;j++) {
            if(j===1)continue;
            if(i===2)break outer;
            total+=i+j;
        }
    }
    await 0;
    for(let i=0;i<5;i++) {
        if(i===1)continue;
        if(i===4)break;
        total+=i;
    }
    return total;
}
synchronousJumps().then(value=>console.log('synchronous-jumps',value));

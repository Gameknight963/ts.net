let value:number|undefined=2;
console.log('local',value!++,++value!,value);
let unknown:unknown=5;
(unknown as number)+=2;
console.log('asserted compound',unknown);
(unknown as number)=9;
console.log('asserted store',unknown);
const values:(number|undefined)[]=[3];
let indices=0;
function index():number {indices++;return 0;}
console.log('element',values[index()]!++,values[0],indices);
let stored:number|undefined=4,gets=0,sets=0,receivers=0;
const accessor={get value():number|undefined {gets++;return stored;},set value(next:number|undefined){sets++;stored=next;}};
function receiver():typeof accessor {receivers++;return accessor;}
console.log('accessor',++receiver().value!,stored,gets,sets,receivers);
let missing:number|undefined;
missing!??=12;
console.log('logical',missing);
function increment(parameter:number):number {parameter!++;return parameter;}
console.log('written parameter',increment(6));
function loop():number {
    let total=0;
    for(let i=0;i<6;i++){i!++;total+=i;}
    return total;
}
console.log('written induction variable',loop());
async function awaitedTargets():Promise<void> {
    let current:number|undefined=2,reads=0,writes=0,owners=0;
    const target={get value():number|undefined {reads++;return current;},set value(next:number|undefined){writes++;current=next;}};
    async function owner():Promise<typeof target> {owners++;return target;}
    (await owner()).value!+=await 3;
    const previous=(await owner()).value!++;
    let absent:number|undefined;
    absent!??=await 9;
    console.log('awaited targets',previous,current,reads,writes,owners,absent);
}
awaitedTargets();

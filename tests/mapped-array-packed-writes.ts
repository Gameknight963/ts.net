export {};
function make():number[]{return [1,2,3,4].map(value=>value);}
function read(values:any[],index:number):any{return values[index];}
function write(values:number[],limit:number):void{for(let index=0;index<limit;index++)values[index]=index+11;}
const cached=make();
const alias:any[]=cached;
console.log("cached",read(alias,0),read(alias,1),read(alias,2),read(alias,3));
write(cached,2.5);
console.log("after",read(alias,0),read(alias,1),read(alias,2),read(alias,3));
write(cached,4);
console.log("again",read(alias,0),read(alias,1),read(alias,2),read(alias,3));
for(const limit of [0,-1,NaN]){const values=make();write(values,limit);console.log("empty",values.join(","));}
const growing=make();write(growing,4.5);console.log("grow",growing.length,growing.join(","));
for(const initial of ["text",undefined]){
    const values=make();
    (values as any)[1]=initial;
    write(values,1.5);
    console.log("mixed",read(values,0),read(values,1),typeof read(values,1),values[2]);
}
const hole=make();delete (hole as any)[1];write(hole,1.5);
console.log("hole",hole.join(","),Object.hasOwn(hole,"1"));
const frozen=make();Object.freeze(frozen);
try{write(frozen,4);}catch(error){console.log("frozen",(error as Error).name,frozen[0]);}
const locked=make();Object.defineProperty(locked,"1",{writable:false});
try{write(locked,4);}catch(error){console.log("readonly",(error as Error).name,locked[0],locked[1]);}
let stored=0,sets=0;
const accessor=make();Object.defineProperty(accessor,"1",{get(){return stored;},set(value:number){sets++;stored=value;},configurable:true});
write(accessor,1.5);
console.log("accessor",accessor[0],accessor[1],sets);
const sealed=make();Object.seal(sealed);write(sealed,4);console.log("sealed",sealed.join(","));
const fixedLength=make();Object.defineProperty(fixedLength,"length",{writable:false});write(fixedLength,4);console.log("length",fixedLength.join(","));
const inherited=make();delete (inherited as any)[1];
const prototype=Object.create(Array.prototype);let inheritedValue=0;
Object.defineProperty(prototype,"1",{get(){return inheritedValue;},set(value:number){inheritedValue=value;},configurable:true});
Object.setPrototypeOf(inherited,prototype);write(inherited,1.5);
console.log("prototype",inherited[0],inherited[1],Object.hasOwn(inherited,"1"));
function overwrite(values:number[],value:number):void{for(let index=0;index<4;index++)values[index]=value;}
for(const value of [NaN,-0,Infinity,undefined]){
    const values=make();overwrite(values,value as any);
    console.log("value",Number.isNaN(values[0]),Object.is(values[0],-0),values[0]===undefined,values[0]);
}
const special=make();
for(let index=0;index<4;index++)special[index]=index===0?-0:index===1?0/0:index===2?1/0:-1/0;
console.log("special",Object.is(special[0],-0),Number.isNaN(special[1]),special[2],special[3]);

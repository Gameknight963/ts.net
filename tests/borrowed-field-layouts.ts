export {};
const owner={base:2,read(amount:number):number{return this.base+amount;}};
const read=owner.read as (this:any,amount:number)=>number;
function call(value:{base:number},amount:number):number {return read.call(value,amount);}
const narrow={base:4};
const wide={base:6,extra:true};
console.log(owner.read(3),call(narrow,3),call(wide,3),read.apply(narrow,[5]));
let gets=0;
const accessor={get base(){gets++;return 11;}};
console.log(call(accessor,3),gets);
const inherited=Object.create({base:13});
console.log(read.call(inherited,3));
for(const value of [undefined,NaN,-0,Infinity,17]){
    const alias:any=narrow;
    alias.base=value;
    console.log(call(narrow,0),read.call(narrow,1),narrow.base===undefined,Object.is(narrow.base,-0));
}
class Other {base=19;}
console.log(call(new Other(),3));
console.log(read.call({},3));
try {read.call(null,3);} catch(error){console.log((error as Error).name);}
let receivers=0,argumentsRead=0;
function receiver():{base:number}{receivers++;return wide;}
function argument():number{argumentsRead++;return 23;}
console.log(read.call(receiver(),argument()),receivers,argumentsRead);

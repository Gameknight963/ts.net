export {};
function read(items:any[],index:number):any{return items[index];}
function number(items:any[],index:number):number{return (read(items,index) as number)+0;}
function reciprocal(items:any[],index:number):number{return 1/(read(items,index) as number);}
const mixed:any[]=[7,"text"];
console.log(number(mixed,0),reciprocal(mixed,0));
for(const value of [undefined,NaN,-0,Infinity,17]){
    mixed[0]=value;
    console.log(number(mixed,0),reciprocal(mixed,0));
}
const numeric=[3,5];
console.log(number(numeric,1));
(mixed as any)["0.5"]=19;
(mixed as any)["-1"]=23;
(mixed as any).undefined=29;
console.log(number(mixed,0.5),number(mixed,-1),number(mixed,undefined as any));
let reads=0;
Object.defineProperty(mixed,"0",{get(){reads++;return 31;},configurable:true});
console.log(number(mixed,0),reads);
delete mixed[0];
const prototype=Object.create(Array.prototype);
prototype[0]=37;
Object.setPrototypeOf(mixed,prototype);
console.log(number(mixed,0),reads);
console.log(number(mixed,99));
let order="";
function items():any[]{order+="a";return numeric;}
function index():number{order+="i";return 1;}
console.log((read(items(),index()) as number)+0,order);
function shifted(items:any[],index:number):any{return items[index+1];}
function defaults(items:any[],index=1):any{return items[index];}
console.log((shifted(numeric,0) as number)+0,(defaults(numeric) as number)+0);
let dynamic=read;
dynamic=(items:any[],index:number):any=>items[index+1];
console.log((dynamic(numeric,0) as number)+0);
function observable(items:any[],index:number):any{reads++;return items[index];}
console.log((observable(numeric,0) as number)+0,reads);
function unknown(items:unknown[],index:number):unknown{return items[index];}
console.log((unknown(numeric,1) as number)+0);
order="";
try {console.log((read(null as any,index()) as number)+0);} catch(error){console.log((error as Error).name,order);}
const unusual:any[]=[];
(unusual as any).NaN=41;
(unusual as any).Infinity=43;
(unusual as any)[4294967295]=47;
console.log(number(unusual,NaN),number(unusual,Infinity),number(unusual,4294967295));
function work():number {
    const input:number[]=[];
    for(let index=0;index<10000;index++)input.push(index);
    const values:(number|string)[]=input.map(value=>value);
    values[9999]="text";
    let sum=0;
    for(let index=0;index<9999;index++)sum+=read(values,index) as number;
    return sum;
}
console.log(work());

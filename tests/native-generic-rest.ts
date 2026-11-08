export {};
interface FirstSize {readonly size:number;}
function first<T extends FirstSize>(...values:T[]):FirstSize{return values[0];}
const map=new Map([["a",1]]);const second=new Map([["b",2],["c",3]]);
console.log("first",first(map).size,first(second,map)===second);
interface SpreadSize {readonly size:number;}
function spread<T extends SpreadSize>(...values:T[]):SpreadSize{return values[0];}
const maps=[map,second];
console.log("spread",spread(...maps).size,spread(...maps)===map);
interface LabeledSize {readonly size:number;}
function labeled<T extends LabeledSize>(label:string,...values:T[]):LabeledSize{console.log("label",label,values.length);return values[0];}
console.log("named argument",labeled("maps",map,second).size);
interface TupleSize {readonly size:number;}
function tuple<T extends TupleSize>(...values:[T,T]):TupleSize{return values[1];}
console.log("tuple",tuple(map,second).size,tuple(map,second)===second);
interface ConstructorSize {readonly size:number;}
class Box<T extends ConstructorSize>{
    value:T;
    constructor(...values:T[]){this.value=values[0];}
    read():ConstructorSize{return this.value;}
}
const box=new Box(map,second);console.log("constructor",box.read().size,box.read()===map);
const ordinary={size:4};console.log("ordinary",first(ordinary).size,spread(ordinary)===ordinary);

interface TailSize {readonly size:number;}
function tail<T extends TailSize>(...values:[string,...T[]]):TailSize{return values[values.length-1] as T;}
console.log("tuple tail",tail<TailSize>("label",{size:0},map).size,tail("spread",...maps)===second);
interface BoundedSize {readonly size:number;}
function bounded<T extends BoundedSize[]>(...values:T):BoundedSize{return values[0];}
console.log("bounded rest",bounded(map).size);
interface PairSize {readonly size:number;}
interface PairLength {readonly length:number;}
function mixed<A extends PairSize,B extends PairLength>(...values:[A,A,B]):PairLength{return values[2];}
const pair=[ordinary,ordinary] as const;
console.log("spread offset",mixed(...pair,"hello").length);

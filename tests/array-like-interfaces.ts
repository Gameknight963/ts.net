export {};
interface Sequence<T> extends ArrayLike<T>{}
function sum(values:Sequence<number>):number{let result=0;for(let i=0;i<values.length;i++)result+=values[i];return result;}
console.log(sum([1,2,3]),sum(new Uint8Array([4,5,6])));
type ReadonlySequence=Readonly<ArrayLike<number>>;
function size(values:ReadonlySequence):number{return values.length;}
console.log(size(new Float64Array([7,8])));
interface LabeledSequence<T> extends Sequence<T>{label?:string;}
function labeled(values:LabeledSequence<number>){return [values.length,values[0],values.label].join("|");}
console.log("optional fields",labeled(new Uint8Array([9,10])),labeled({0:11,length:1,label:"record"}));
type PartialSequence=Partial<ArrayLike<number>>;
function partial(values:PartialSequence){return (values.length??0)+(values[0]??0);}
console.log("partial",partial(new Float32Array([2,3])),partial({}));
interface IterableSequence<T> extends Sequence<T>,Iterable<T>{}
function iterate(values:IterableSequence<number>){let total=0;for(const value of values)total+=value;return total;}
console.log("iterable",iterate([3,4,5]),iterate(new Int16Array([6,7])));
function text(values:Sequence<string>){let result="";for(let i=0;i<values.length;i++)result+=values[i];return result;}
console.log("text",text("hello"),text(["a","b"]));
function forward<T>(values:Readonly<Sequence<T>>):Readonly<Sequence<T>>{return values;}
const bytes=new Uint8Array([1,2,3]);console.log("identity",forward(bytes)===bytes,forward("abc")==="abc");
type Intersection=ArrayLike<number>&{label?:string};
function first(values:Intersection):number{return values[0];}
console.log("intersection",first(new Float64Array([8])));

function truth(values: Sequence<boolean>): boolean { return values[0]; }
console.log("booleans", truth([true]), truth({0: false, length: 1}));
function missing(values: Sequence<number | undefined>): string {
    return [values[0] ?? "missing", values[1] ?? "missing"].join("|");
}
console.log("nullable elements", missing([undefined, 12]), missing(new Float64Array([13])));

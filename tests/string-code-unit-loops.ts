export {};
function sum(text:string):number {
    let result=0;
    for(let i=0;i<text.length;i++)result+=text.charCodeAt(i);
    return result;
}
console.log(sum(''),sum('abc'),sum('\uD83D\uDE00\uD800\uDC00'));
function tail(text:string):number {
    let result=0;for(let i=2;i<text.length;i++)result+=text.charCodeAt(i);return result;
}
console.log(tail('a'),tail('abcd'));
const original=Object.getOwnPropertyDescriptor(String.prototype,'charCodeAt')!;
try {
    String.prototype.charCodeAt=function(index:number):number{return index+10;};
    console.log('replacement',sum('abc'));
    let gets=0;
    Object.defineProperty(String.prototype,'charCodeAt',{configurable:true,get(){gets++;return original.value;}});
    console.log('getter',sum('abc'),gets);
    gets=0;console.log('empty getter',sum(''),gets);
    gets=0;
    Object.defineProperty(String.prototype,'charCodeAt',{configurable:true,get(){
        gets++;Object.defineProperty(String.prototype,'charCodeAt',{value:function():number{return 20;},writable:true,configurable:true});
        return original.value;
    }});
    console.log('changing getter',sum('abc'),gets);
} finally {Object.defineProperty(String.prototype,'charCodeAt',original);}
function changeMethod():void {String.prototype.charCodeAt=function():number{return 7;};}
try {
    const text='abc';let result=0;
    for(let i=0;i<text.length;i++){result+=text.charCodeAt(i);changeMethod();}
    console.log('callback',result);
} finally {Object.defineProperty(String.prototype,'charCodeAt',original);}
let changing='abcd';let changedSum=0;
for(let i=0;i<changing.length;i++){changedSum+=changing.charCodeAt(i);changing='xy';}
console.log('receiver',changedSum);
const visible='abc';let visibleSum=0;
for(var visibleIndex=0;visibleIndex<visible.length;visibleIndex++)visibleSum+=visible.charCodeAt(visibleIndex);
console.log('visible counter',visibleSum,visibleIndex);
const fractional='abc';let fractionalSum=0;
for(let i=0;i<fractional.length;i+=0.5)fractionalSum+=fractional.charCodeAt(i);
console.log('fractional counter',fractionalSum);
const shifted='abc';let shiftedSum=0;
for(let i=0;i<shifted.length;i++)shiftedSum+=shifted.charCodeAt(i+1);
console.log('shifted',Number.isNaN(shiftedSum));
const objectDescriptor=Object.getOwnPropertyDescriptor(Object.prototype,'charCodeAt');
try {
    Object.defineProperty(Object.prototype,'charCodeAt',{value:function():number{return 9;},configurable:true});
    delete (String.prototype as any).charCodeAt;
    console.log('inherited',sum('abc'));
} finally {
    Object.defineProperty(String.prototype,'charCodeAt',original);
    if(objectDescriptor)Object.defineProperty(Object.prototype,'charCodeAt',objectDescriptor);
    else delete (Object.prototype as any).charCodeAt;
}
let threw=false;try{sum(undefined as any);}catch{threw=true;}console.log('undefined',threw);
function early():number {let result=0;for(let i=0;i<late.length;i++)result+=late.charCodeAt(i);return result;}
threw=false;try{early();}catch{threw=true;}console.log('uninitialized',threw);
const late='abc';console.log('initialized',early());

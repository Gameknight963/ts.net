const callbacks:Array<(value:number)=>boolean>=[value=>value===3];
console.log('function value',callbacks[0](3),callbacks[0](4));
const defaults:Array<(value:number)=>string>=[(value:number,other:number=7)=>String(value+other)];
console.log('default second',defaults[0](2));
const missing:Array<(value:number)=>boolean>=[(value:number,other?:number)=>value===2&&other===undefined];
console.log('missing second',missing[0](2));
const receiver={value:9,check(value:number):boolean{return this.value===value;}};
console.log('method receiver',receiver.check(9),receiver['check'](8));
const receiverFree={check(value:number):boolean{return value===9;}};
console.log('method without receiver',receiverFree.check(9));
const argumentsFunctions:Array<(value:number)=>boolean>=[function(value:number):boolean{return arguments.length===1&&arguments[0]===value;}];
console.log('arguments',argumentsFunctions[0](2));
const rest:Array<(value:number)=>boolean>=[(...values:number[])=>values.length===1&&values[0]===4];
console.log('rest',rest[0](4));
const primitives:any[]=[String,Boolean];
console.log('native functions',primitives[0](42),primitives[1](0));
const text:any='abcdef';
console.log('native methods',text.slice(2),text.charAt(1),text.at(2),text.includes(123));
const numbers:any=[3,4];
console.log('array fallback',numbers.includes(3),numbers.push(5),numbers.length);
const result={value:1};let sum=0;
const returning:Array<(value:number)=>typeof result>=[value=>{sum+=value;return result;}];
console.log('object result',returning[0](2)===result,sum);
const voidFunctions:Array<(value:number)=>void>=[value=>{sum+=value;}];
console.log('void result',voidFunctions[0](3),sum);
let lookup=0,argument=0;
const getter={get check(){lookup++;return (value:number):boolean=>value===1;}};
console.log('evaluation',getter.check(++argument),lookup,argument);
const invalid:any={check:0};
try{invalid.check(++argument);}catch(error){console.log('invalid',(error as Error).name,argument);}
{
    type Method=(this:Method[],value:number)=>boolean;
    const methods:Method[]=[function(this:Method[],value:number):boolean{return this===methods&&value===3;}];
    console.log('array method receiver',methods[0](3));
    let reads=0;
    Object.defineProperty(methods,'0',{get(){reads++;return function(this:Method[],value:number):boolean{return this===methods&&value===4;};},configurable:true});
    console.log('array method getter',methods[0](4),reads);
    const inherited:Method[]=[];
    const proto=Object.create(Array.prototype);
    proto[0]=function(this:Method[],value:number):boolean{return this===inherited&&value===5;};
    Object.setPrototypeOf(inherited,proto);
    console.log('array inherited method',inherited[0](5));
    const selected=[(value:number):boolean=>value===1];
    function replace():number {selected[0]=()=>false;return 1;}
    console.log('array method selection',selected[0](replace()),selected[0](1));
    const arrayLike:any={0:(value:number):boolean=>value===6};
    console.log('array-like method',arrayLike[0](6));
}
{
    const raw:any={calculate(first:number,second:number):number{return first+second;}};
    console.log('raw missing argument',Number.isNaN(raw.calculate(1)),Number.isNaN(raw.calculate(undefined as unknown as number)));
    const observes:any={calculate(first:number,second:number):number{return second;}};
    console.log('observed missing argument',observes.calculate(1)===undefined);
    const testsUndefined:any={calculate(first:number,second:number):number{return second===undefined?7:second;}};
    console.log('tested missing argument',testsUndefined.calculate(1));
}

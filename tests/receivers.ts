function read(this:{value:number}, increment=0):number { return this.value+increment; }
const first={value:10,read};
const second={value:20,read};
console.log(first.read(),second.read(2),read.length);
const loose:any=read;
try { loose(); } catch(error) { console.log(error instanceof TypeError); }
const methods={
    value:3,
    increment(amount:number):number { this.value+=amount;return this.value; },
    capture():()=>number { return ()=>this.value; },
    nested():()=>()=>number { return ()=>()=>this.value; },
    plain():boolean { function inner(this:unknown):boolean{return this===undefined;} return inner(); },
    defaults(this:{value:number},amount=this.value):number{return amount;},
    rest(...values:number[]):number{return this.value+values[0]+values[1];}
};
console.log(methods.increment(4),methods.increment.length,methods.capture.length);
const capture=methods.capture();
methods.value=8;
console.log(capture(),methods.nested()()(),methods.plain(),methods.defaults(),methods.rest(1,2));
const argumentsList:[number]=[2];
console.log(methods.increment(...argumentsList));
const dynamic:any=methods;
console.log(dynamic.increment(1),dynamic.increment(1,2));
const other={value:40,increment:methods.increment};
console.log(other.increment(2),methods.value,capture());
const anonymous={value:50,read:function(this:{value:number},add=1){return this.value+add;}};
console.log(anonymous.read(),anonymous.read.length);

const recursive={value:6,run:function again(this:any,n:number):number {
    if(n>0)return again(n-1);
    return this===undefined?9:this.value;
}};
console.log(recursive.run(0),recursive.run(2));
const retained={value:17,make(this:{value:number},read=()=>this.value):()=>number{return read;}};
const retainedRead=retained.make();retained.value=19;console.log(retainedRead());
const detached=methods.increment;
try { detached(1); } catch(error) { console.log(error instanceof TypeError); }
function outer(this:{value:number}):()=>boolean {
    const arrow=()=>this.value;
    function inner(this:unknown):boolean { return this===undefined; }
    console.log(arrow());return inner;
}
const outerOwner={value:23,outer};console.log(outerOwner.outer()());

const primitive={value:31,valueOf():number{return this.value;},toString():string{return "v"+this.value;}};
console.log(+primitive,primitive.toString());

const value={x:5,["read"](){return this.x;},"str"(){return this.x+1;}};
console.log(value.read(),value.str(),value.read.name,value.str.name);

let keyEffects=0;
function methodKey():"dynamic" { keyEffects++;return "dynamic"; }
const computed={value:37,[methodKey()](){return this.value;},7(){return this.value+1;},"__proto__"(){return this.value+2;}};
console.log(computed.dynamic(),computed[7](),computed.__proto__(),computed.dynamic.name,computed[7].name,keyEffects);

const owner={value:4,read:function named(this:any):any{return this ? named : undefined;}};
console.log(owner.read()===owner.read,owner.read.length,owner.read.name);
const detachedSelf=owner.read;
console.log(detachedSelf()===undefined);

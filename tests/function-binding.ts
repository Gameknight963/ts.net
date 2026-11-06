function add(this:{base:number},first:number,second=2):number{return this.base+first+second;}
const owner={base:10};console.log(add.call(owner,1),add.apply(owner,[3,4]));
const bound=add.bind(owner,5);console.log(bound(),bound(6),bound.name,bound.length);
owner.base=20;console.log(bound());
const rebound=bound.bind({base:100},7);console.log(rebound(),rebound.name,rebound.length);
const another={base:100,method:bound};console.log(another.method());
function strictThis(this:unknown){return this;}
console.log(strictThis.call(null)===null,strictThis.call(undefined)===undefined,strictThis.call(7)===7);
function count(...values:any[]){return values.length;}
console.log(count.apply(null,[]),count.apply(null,null as any),count.apply(null,undefined as any));
const arrayLike:any={0:'a',1:'b',length:2.8};console.log(count.apply(null,arrayLike));
function inspect(this:unknown,first:any,second:any){console.log(first===undefined,second===undefined);return 1;}
inspect.apply(null,[,,]);
const loose:any=count;try{loose.apply(null,'abc');}catch(error){console.log(error instanceof TypeError);}
const detached:any=add.call;try{detached(owner,1);}catch(error){console.log(error instanceof TypeError);}
console.log(add.call===count.call,add.apply.name,add.apply.length,add.bind.length,Object.hasOwn(add,'call'));
const looseCall:any=add.call;console.log(looseCall.call(add,owner,2,3));
const arrowOwner={base:30,make(){return ()=>this.base;}};const arrow=arrowOwner.make();console.log(arrow.bind({base:100})(),arrow.call({base:200}));
class BoundCounter {constructor(public value:number,public step=2){}next(){return this.value+this.step;}}
const Bound=BoundCounter.bind(null,4);const instance=new Bound(3);
console.log(instance.next(),instance instanceof BoundCounter,instance instanceof Bound,Bound.length,Bound.name,Object.hasOwn(Bound,'prototype'));
const BoundAgain=Bound.bind({value:100},6);const again=new BoundAgain();console.log(again.next(),again instanceof BoundAgain,again instanceof BoundCounter);
try{(Bound as any)();}catch(error){console.log(error instanceof TypeError);}
console.log('abc'.charCodeAt.call('xyz',1),'abc'.slice.apply('wxyz',[1,3]));
const boundAt=[1,2].at.bind([3,4]);console.log(boundAt(-1));
const BoundError:any=TypeError.bind(null,'bound error');const boundError=new BoundError();console.log(boundError.message,boundError instanceof TypeError,boundError instanceof BoundError);
let applyOrder='';const observable:any={get length(){applyOrder+='L';return 2;},get 0(){applyOrder+='A';return 3;},get 1(){applyOrder+='B';return 4;}};
console.log(add.apply(owner,observable),applyOrder);
function numberThis(this:number):number{return this;}
console.log(numberThis.call(7),numberThis.apply(8,[]),numberThis.bind(9)());
class ParentStatics {static value=40;static read(){return this.value;}}
class ChildStatics extends ParentStatics {static own=50;}
const BoundStatics=ChildStatics.bind(null);
console.log((BoundStatics as any).value,(BoundStatics as any).read(),(BoundStatics as any).own===undefined,Object.hasOwn(BoundStatics,'value'));
const boundArrow:any=(()=>1).bind(null);try{new boundArrow();}catch(error){console.log(error instanceof TypeError);}
let metadataReads='';function lengthKey():string{return 'length';}function nameKey():string{return 'name';}
class BindingMetadata {static get [lengthKey()](){metadataReads+='L';return 3.8;}static get [nameKey()](){metadataReads+='N';return 'metadata';}}
const metadataBound=(BindingMetadata as any).bind(null,1);console.log(metadataBound.length,metadataBound.name,metadataReads);
const dynamicOwner={base:1};const dynamicBound=add.bind(dynamicOwner,2);const arbitraryReceiver:any={base:100};
console.log(dynamicBound.call(arbitraryReceiver,3),dynamicBound.apply(arbitraryReceiver,[4]));
const invalidCall:any=add.call;try{invalidCall.call({},null);}catch(error){console.log(error instanceof TypeError);}
const named:any=count;named.call=()=>99;console.log(named.call(),Object.hasOwn(named,'call'));
const countBound=count.bind(null,undefined);console.log(countBound(),countBound(1),countBound(1,2));
const countTwo=count.bind(null,1,2);console.log(countTwo(),countTwo(3),countTwo(3,4));
function classify(value:any):number{return value===null?1:value===true?2:3;}
console.log(classify.bind(null,null)(),classify.bind(null,true)(),classify.bind(null,'text')());
const countZero=count.bind(null);console.log(countZero(),countZero(1),countZero(1,2));

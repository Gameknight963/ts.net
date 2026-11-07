export {};
const parent={base:3,read(this:any):number{return this.own+this.base;},get value():number{return (this as any).own*2;},set value(value:number){(this as any).own=value/2;}};
const home={__proto__:parent,own:5,read():number{return super.read();},get value():number{return super.value;},set value(value:number){super.value=value;},arrow(){return ()=>super.read();},args(n:number):number{return super.read()+arguments[0];}};
console.log("method",home.read(),home.value,home.args(2));home.value=20;console.log("setter",home.own,home.value);
const borrowed={own:7,base:100,read:home.read};console.log("borrow",borrowed.read(),home.read.call({own:9,base:10}));
const arrow=home.arrow();console.log("arrow",arrow());
Object.setPrototypeOf(home,{read(this:any){return this.own*10;},get value(){return 99;}});
console.log("changed",home.read(),borrowed.read(),arrow(),home.value);
function make(base:number){return {__proto__:{base},read():number{return super.base;}};}
const first=make(1),second=make(2);console.log("independent",first.read(),second.read(),first.read());
const computed={__proto__:parent,["read"]():number{return super.read();},own:4};console.log("computed",computed.read());
const update={__proto__:{value:3},read(){return super.value++;}};console.log("update",update.read(),(update as any).value);
async function main():Promise<void>{
 const asyncHome={__proto__:parent,own:6,async read():Promise<number>{await Promise.resolve();return super.read();},*values(){yield super.read();yield (()=>super.value)();},async *asyncValues(){await Promise.resolve();yield super.read();}};
 console.log("async",await asyncHome.read());console.log("generator",Array.from(asyncHome.values()).join("|"));
 for await(const value of asyncHome.asyncValues())console.log("async generator",value);
 const awaited={__proto__:await Promise.resolve(parent),own:8,read(){return super.read();}};console.log("awaited home",awaited.read());
}
main().catch(error=>console.log("failure",String(error)));

const defaults={__proto__:{value:12},read(value:number=super.value){return value;}};console.log("default",defaults.read(),defaults.read(4));
const nested={__proto__:{key:"read",Base:class{base=2;}},build(){return class extends super.Base{[super.key](){return 9;}};}};
const Nested=nested.build();const instance=new Nested();console.log("nested class",instance.base,(instance as any).read());

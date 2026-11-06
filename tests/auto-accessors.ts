class Auto {
    accessor value=1;
    accessor absent:number|undefined;
    other=2;
    static accessor total=3;
    next():number{return ++this.value;}
}
const first=new Auto();const second=new Auto();
console.log(first.value,first.next(),second.value,first.absent,Object.hasOwn(first,'value'),'value' in first,Object.keys(first).length);
first.value=9;console.log(first.value,second.value);
class Child extends Auto {accessor extra=4;}
const child=new Child();console.log(child.next(),child.extra,Child.total);Child.total=8;console.log(Auto.total,Child.total,Object.hasOwn(Child,'total'));
const fake:any={};fake.__proto__=Auto.prototype;
try {console.log(fake.value);}catch(error){console.log('bad-get',error instanceof TypeError);}
try {fake.value=3;}catch(error){console.log('bad-set',error instanceof TypeError);}
let order='';
const names={get key():'computed'{order+='key,';return 'computed';}};
class Computed {
    before=(order+='before,',1);
    accessor [names.key]=(order+='auto,',5);
    after=(order+='after,',2);
    static accessor count=(order+='static,',6);
    static {order+='block,';}
}
console.log('definition',order,Computed.count);
const computed=new Computed();new Computed();
console.log('computed',computed.computed,order,Object.keys(computed).length,Object.hasOwn(Computed,'count'));
class Override extends Auto {override accessor value=10;readBase():number{return super.value;}writeBase(v:number):void{super.value=v;}}
const override=new Override();console.log('override',override.value,override.readBase());override.writeBase(20);console.log(override.value,override.readBase());
class Early {before=this.read();accessor value=2;read():number{try{return this.value;}catch(error){console.log('early',error instanceof TypeError);return -1;}}}
const early=new Early();console.log(early.before,early.value);
class FunctionValues {accessor callback=()=>1;accessor nested=class{static captured=this.name;};}
const functionValues=new FunctionValues();
console.log('names',functionValues.callback.name,functionValues.nested.name,functionValues.nested.captured);
const spread={...first};const {value:destructured,...rest}=first;
console.log('spread',Object.keys(spread).length,Object.keys(rest).length,destructured,Object.hasOwn(spread,'value'));
function make(){return class {accessor value=1;};}
const One=make();const Two=make();const one=new One();const two=new Two();
const fakeOne:any={};fakeOne.__proto__=One.prototype;
try {console.log(fakeOne.value);}catch(error){console.log('definition-brand',error instanceof TypeError);}
one.value=7;console.log('separate',one.value,two.value);
const savedPrototype:any=(two as any).__proto__;(two as any).__proto__=One.prototype;
try {console.log(two.value);}catch(error){console.log('other-definition',error instanceof TypeError);}
(two as any).__proto__=savedPrototype;
const shared:any={};function Returning(this:any){return shared;}
class Stamped extends (Returning as any) {accessor value=31;}
new (Stamped as any)();shared.__proto__=Stamped.prototype;console.log('stamped',shared.value);
try {new (Stamped as any)();}catch(error){console.log('duplicate-brand',error instanceof TypeError);}
class StaticEarly {
    static first=this.read();
    static accessor value=5;
    static read():number {try{return this.value;}catch(error){console.log('static-early',error instanceof TypeError);return -1;}}
}
console.log('static-ready',StaticEarly.first,StaticEarly.value);

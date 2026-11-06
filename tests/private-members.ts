class Counter {
    #value=1;
    #text:string|undefined;
    #double():number{return this.#value*2;}
    get #access():number{return this.#value;}
    set #access(value:number){this.#value=value;}
    accessor #auto=4;
    read():number{return this.#value;}
    next():number{return ++this.#value;}
    call():number{return this.#double();}
    update(value:number):number {this.#access=value;this.#auto++;return this.#access+this.#auto;}
    static has(value:any):boolean{return #value in value;}
    static read(value:any):number{return value.#value;}
    text():string|undefined{return this.#text;}
}
const first=new Counter();const second=new Counter();
console.log(first.read(),first.next(),first.call(),first.update(10),second.read(),first.text());
console.log(Counter.has(first),Counter.has({}),Object.keys(first).length,Object.hasOwn(first,'#value'));
try{Counter.read({});}catch(error){console.log('bad-read',error instanceof TypeError);}
try{Counter.has(3);}catch(error){console.log('bad-in',error instanceof TypeError);}
class Static {
    static #value=2;
    static #method():number{return this.#value;}
    static read():number{return this.#method();}
    static next():number{return ++this.#value;}
}
class Derived extends Static {}
console.log('static',Static.read(),Static.next());
try{console.log(Derived.read());}catch(error){console.log('static-brand',error instanceof TypeError);}
class Child extends Counter {#value=30;readOwn():number{return this.#value;}}
const child=new Child();console.log('child',child.read(),child.readOwn(),child.call());
class Presence {
    snapshot=this.check();
    #field=1;
    accessor #auto=2;
    #method():number{return 3;}
    check():string{return ''+(#field in this)+','+(#auto in this)+','+(#method in this);}
    static snapshot=this.checkStatic();
    static #staticField=4;
    static #staticMethod():number{return 5;}
    static checkStatic():string{return ''+(#staticField in this)+','+(#staticMethod in this);}
}
console.log('presence',new Presence().snapshot,Presence.snapshot);
class Optional {
    #value=6;
    #method(value=1,...rest:number[]):number{return this.#value+value+(rest[0]??0);}
    #callback:(()=>number)|undefined;
    read(value:Optional|null):number|undefined{return value===null?undefined:value.#value;}
    call(value:Optional|null):number|undefined{return value===null?undefined:value.#method?.(2,3);}
    callback():number|undefined{return this.#callback?.();}
    spread(values:number[]):number{return this.#method(...values);}
}
const optional=new Optional();console.log('optional',optional.read(optional),optional.read(null),optional.call(optional),optional.call(null),optional.callback(),optional.spread([4,5]));
let effects='';
class Order {
    #value=1;
    assign(target:any):void {target.#value=(effects+='rhs,',2);}
    add(target:any):void {target.#value+=(effects+='rhs,',2);}
}
const order=new Order();try{order.assign({});}catch(error){console.log('assign-order',effects,error instanceof TypeError);}
effects='';try{order.add({});}catch(error){console.log('add-order',effects,error instanceof TypeError);}
class Box<T> {#value:T;constructor(value:T){this.#value=value;}read():T{return this.#value;}}
console.log('generic',new Box(9).read(),new Box('text').read());
function make(){return class {#value=1;read(target:any):number{return target.#value;}};}
const A=make();const B=make();const a=new A();const b=new B();
try{a.read(b);}catch(error){console.log('different-definition',error instanceof TypeError);}
(first as any)['#value']=99;console.log('string-key',first.read(),(first as any)['#value'],Object.keys(first).length);
class Names {#callback=()=>1;#nested=class{static captured=this.name;};read():string{return this.#callback.name+','+this.#nested.name+','+this.#nested.captured;}}
console.log('names',new Names().read());
class Outer {
    #value=40;
    Inner=class {read(value:Outer):number{return value.#value;}};
    Shadow=class {#value=50;read(value:any):number{return value.#value;}};
}
const outer=new Outer();console.log('nested',new outer.Inner().read(outer));
const shadow=new outer.Shadow();console.log('shadow',shadow.read(shadow));
try{shadow.read(outer);}catch(error){console.log('shadow-brand',error instanceof TypeError);}
const shared:any={};function Returning(this:any){return shared;}
class Stamped extends (Returning as any) {#value=21;static read(value:any):number{return value.#value;}static has(value:any):boolean{return #value in value;}}
new (Stamped as any)();console.log('stamped',Stamped.read(shared),Stamped.has(shared),Object.keys(shared).length);
try{new (Stamped as any)();}catch(error){console.log('duplicate',error instanceof TypeError);}
class Access {
    #value=0;
    set #write(value:number){this.#value=value;}
    get #read():number{return this.#value;}
    use():number {this.#write=8;return this.#read;}
}
console.log('accessors',new Access().use());
class Forgery {
    #value=0;
    read():number {const fake:any={'#value':100};return fake.#value;}
    update():number {const fake:any={'#value':100};return ++fake.#value;}
}
try{new Forgery().read();}catch(error){console.log('forged-read',error instanceof TypeError);}
try{new Forgery().update();}catch(error){console.log('forged-update',error instanceof TypeError);}

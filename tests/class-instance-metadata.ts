export {};
class Item {x=1;name='item';read(){return this.x;}}
const a=new Item(),b=new Item(),c=new Item(),d=new Item();
Object.defineProperty(a,'x',{get(){return 7;},enumerable:false,configurable:true});
console.log('accessor isolation',a.x,b.x,c.x,Object.keys(a).join(','),Object.keys(b).join(','));
Object.freeze(a);b.x=2;
console.log('freeze isolation',Object.isFrozen(a),Object.isExtensible(b),a.x,b.x,b instanceof Item);
Object.setPrototypeOf(c,{read(){return 99;}});
console.log('prototype isolation',c.read(),d.read(),c instanceof Item,d instanceof Item);
(d as any).extra=4;delete (d as any).extra;
console.log('extra retains class',d.read(),d instanceof Item,Object.getPrototypeOf(d)===Item.prototype);
Object.defineProperty(d,'name',{value:'changed',writable:false,configurable:false});
console.log('descriptor isolation',d.name,b.name,Object.getOwnPropertyDescriptor(d,'name')!.writable,Object.getOwnPropertyDescriptor(b,'name')!.writable);
function make(label:string){return class {name=label;};}
const A=make('a'),B=make('b');const aa=new A(),bb=new B();
console.log('owner isolation',aa.name,bb.name,aa instanceof A,aa instanceof B,bb instanceof A,bb instanceof B);
Object.setPrototypeOf(aa,null);
console.log('factory prototype isolation',aa instanceof A,bb instanceof B,Object.getPrototypeOf(bb)===B.prototype);
class Base {base=3;get value(){return this.base;}}
class Derived extends Base {own=4;}
const e=new Derived(),f=new Derived();Object.seal(e);f.base=8;
console.log('derived isolation',e.value,f.value,e instanceof Base,e instanceof Derived,f instanceof Base,f instanceof Derived,Object.isSealed(e),Object.isSealed(f));

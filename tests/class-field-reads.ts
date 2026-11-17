class Item { value=1; name="item"; enabled=true; }
function read(item:Item):void { console.log(item.value,item.name,item.enabled); }
const item=new Item();read(item);
item.value=2;item.name="changed";item.enabled=false;read(item);
(item as any).value=undefined;(item as any).name=undefined;(item as any).enabled=undefined;read(item);
item.value=3;(item as any).name=null;item.enabled=false;read(item);
const descriptor=new Item();
Object.defineProperty(descriptor,"name",{get(){console.log("getter");return "getter value";},configurable:true});read(descriptor);
const missing=new Item();delete (missing as any).name;
Object.defineProperty(Item.prototype,"name",{get(){console.log("inherited");return "prototype";},configurable:true});read(missing);
delete (Item.prototype as any).name;
const fixed=new Item();Object.freeze(fixed);read(fixed);
const sealed=new Item();Object.seal(sealed);sealed.name="sealed";read(sealed);
read({value:7,name:"record",enabled:false});
let reads=0;const proxy=new Proxy(new Item(),{get(target,key,receiver){reads++;return Reflect.get(target,key,receiver);}});read(proxy);console.log("proxy reads",reads);
const revocable=Proxy.revocable(new Item(),{});revocable.revoke();try{read(revocable.proxy);}catch(error){console.log("revoked",(error as Error).name);}
class Child extends Item {extra=4;}read(new Child());
const reordered=new Item();delete (reordered as any).value;reordered.value=9;read(reordered);console.log(Object.keys(reordered).join(","));

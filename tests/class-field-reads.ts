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

class Mutable {
    value = 1;
    text = 'a';
    enabled = false;
    post(): number {return this.value++;}
    pre(): number {return ++this.value;}
    add(rhs: () => number): number {return this.value += rhs();}
    append(rhs: () => string): string {return this.text += rhs();}
    choose(rhs: () => boolean): boolean {return this.enabled ||= rhs();}
}
const mutable = new Mutable();
console.log('updates', mutable.post(), mutable.pre(), mutable.add(() => 4), mutable.value);
console.log('append', mutable.append(() => 'b'));
let choices = 0;
console.log('logical', mutable.choose(() => {choices++; return true;}), mutable.choose(() => {choices++; return false;}), choices);
const borrowed = {value: 10};
console.log('borrowed update', mutable.post.call(borrowed as Mutable), borrowed.value);
const updateEvents: string[] = [];
const described = new Mutable();
Object.defineProperty(described, 'value', {configurable: true,
    get() {updateEvents.push('get'); return 10;},
    set(value: number) {updateEvents.push('set:' + value);}});
console.log('update order', described.add(() => {updateEvents.push('rhs'); return 2;}), updateEvents.join(','));
const changed = new Mutable();
changed.value = 5;
console.log('rhs descriptor', changed.add(() => {
    Object.defineProperty(changed, 'value', {value: 20, writable: true, configurable: true});
    return 3;
}), changed.value);
const absent = new Mutable();
delete (absent as any).value;
try {
    Object.defineProperty(Mutable.prototype, 'value', {configurable: true,
        get() {return 30;}, set(value: number) {updateEvents.push('inherited:' + value);}});
    console.log('inherited update', absent.pre(), updateEvents[updateEvents.length - 1], Object.hasOwn(absent, 'value'));
} finally {delete (Mutable.prototype as any).value;}
(mutable as any).text = undefined;
(mutable as any).enabled = undefined;
console.log('undefined fields', mutable.append(() => 'b'), mutable.choose(() => false));

export {};
function make(value: number): any {
    return class Captured {value = value; count: number; constructor() {this.count = arguments.length;}};
}
const Captured = make(7);
console.log('captured', new Captured().value, new Captured().count);
const Ordinary: any = function(this: any): void {
    this.count = arguments.length;
    this.target = new.target;
    (arguments as any)[0] = 99;
};
const first = new Ordinary();
const second = new Ordinary();
console.log('ordinary', first.count, second.count, second.target === Ordinary);
const seen: any[][] = [];
const Proxied: any = new Proxy(Ordinary, {
    construct(target, args, newTarget) {
        console.log('trap', args.length, newTarget === Proxied, seen.length === 0 || args !== seen[0]);
        args.push(seen.length + 10);
        seen.push(args);
        return Reflect.construct(target, args, newTarget);
    }
});
console.log('proxy', new Proxied().count, new Proxied().count, seen[0]![0], seen[1]![0]);
const Bound: any = Ordinary.bind(null);
console.log('bound', new Bound().count, new Bound().target === Ordinary);
class Base {count: number; constructor() {this.count = arguments.length;}}
class Derived extends Base {extra = 3;}
console.log('derived', new Derived().count, new Derived().extra);
console.log('native', new Array().length, new Map().size, new Set().size);
const Revoked = Proxy.revocable(Ordinary, {});
Revoked.revoke();
try {new Revoked.proxy();} catch (error) {console.log('revoked', (error as Error).name);}

let sequence=0;
function makeOrdinary(seed:number):any{return function(this:any):void{this.value=seed+(++sequence);this.target=new.target;};}
const Simple=makeOrdinary(10);const simpleA=new Simple(),simpleB=new Simple();
console.log('simple zero',simpleA.value,simpleB.value,simpleA!==simpleB,simpleA.target===Simple,simpleA instanceof Simple);
const Returning:any=function(this:any):any{this.discarded=1;return {value:17};};
const returned=new Returning();console.log('returned zero',returned.value,returned instanceof Returning,returned.discarded);
const Primitive:any=function(this:any):any{this.value=19;return 3;};console.log('primitive zero',new Primitive().value);
const Missing:any=function(this:any):void{this.value=23;return undefined;};console.log('undefined zero',new Missing().value);
let initialization='';
function capturedClass(seed:number):any{return class Item {value=(initialization+='field|',seed);constructor(){initialization+='body|';this.value+=2;}};}
const Item=capturedClass(29),item=new Item();console.log('class zero',item.value,item instanceof Item,initialization);
const Untyped:any=Item;console.log('class alias zero',new Untyped().value);
const Changed:any=function(this:any):void{this.value=31;};Changed.prototype={marker:37};const changed=new Changed();console.log('changed prototype zero',changed.value,changed.marker,Object.getPrototypeOf(changed)===Changed.prototype);
const arrow:any=()=>undefined;const method:any={method(){}}.method;
for(const C of [arrow,method]){try{new C();}catch(error){console.log('nonconstructor zero',(error as Error).name);}}

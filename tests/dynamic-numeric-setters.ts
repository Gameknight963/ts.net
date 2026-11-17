export function work(): number {
 const Constructor:any=function(this:any,value:number):void{this.value=value;};
 const item=new Constructor(0) as {value:number};
 let sum=0;
 for(let i=0;i<100000;i++){item.value=i;sum+=item.value;}
 return sum;
}
console.log("sum", work());
const Constructor: any = function(this: any, value: number): void {this.value = value;};
const item = new Constructor(1) as {value: number};
let stored = 0;
Object.defineProperty(item, "value", {get() {return stored;}, set(value: number) {stored = value * 2;}, configurable: true});
item.value = 3;
console.log("accessor", item.value, stored);
Object.defineProperty(item, "value", {value: 5, writable: true, configurable: true});
item.value = 7;
console.log("restored", item.value);
delete (item as any).value;
Object.setPrototypeOf(item, {set value(value: number) {stored = value + 1;}, get value() {return stored;}});
item.value = 11;
console.log("inherited", item.value, stored, Object.hasOwn(item, "value"));
Object.defineProperty(item, "value", {value: 13, writable: true, configurable: true});
Object.seal(item);
item.value = 17;
console.log("sealed", item.value);
Object.freeze(item);
try {item.value = 19;} catch (error) {console.log("frozen", (error as Error).name, item.value);}
const optional = new Constructor(23) as {value: number | undefined};
optional.value = undefined;
console.log("undefined", optional.value, Object.hasOwn(optional, "value"));
optional.value = 29;
console.log("defined", optional.value);

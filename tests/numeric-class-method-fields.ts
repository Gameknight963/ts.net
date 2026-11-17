export {};
class Counter {
  x=2;y=3;
  scale(n:number){return this.x*n+this.y;}
  identity(n:number){return n;}
  get total(){return this.x+this.y;}
  get value(){return this.x;}
  set value(n:number){this.x=n;}
  label(){return 'counter';}
}
let c=new Counter();console.log(c.scale(4),c.identity(7),c.total,c.value,c.label());
c.value=8;console.log(c.scale(2),c.total,c.value);
let a:any=c;a.extra=9;console.log(c.scale(2),c.total,c.value,Object.keys(c).join(','));
let d=new Counter();Object.defineProperty(d,'x',{get(){return 10;},configurable:true});
console.log(d.scale(2),d.total,d.value);
const method=Counter.prototype.scale;
console.log(method.call({x:4,y:5},3));
class Child extends Counter {z=5;}
let child=new Child();console.log(child.scale(2),child.total,child.value,child.z);
Object.defineProperty(Counter.prototype,'total',{get(){return 99;},configurable:true});
console.log(new Counter().total);
class Static {x=1;static x(){return 9;}read(){return this.x;}}
console.log(new Static().read(),Static.x());
class Arrow {x=1;read=()=>this.x;}
console.log(new Arrow().read());
class Private {#x=2;y=3;read(){return this.#x+this.y;}}
console.log(new Private().read());

export function work(count:number,offset:number):number {
 class Value {
  value=offset;
  constructor(value:number){this.value=value;}
  read(scale:number):number{return this.value*scale;}
  get current():number{return this.value;}
 }
 const values:Value[]=[];
 for(let i=0;i<count;i++)values.push(new Value(i));
 let sum=0;for(let i=0;i<values.length;i++)sum+=values[i].read(1)+values[i].current;
 return sum/2;
}
console.log(work(10,2));

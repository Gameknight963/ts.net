export {};
class Point {x=0;y=0;constructor(x=7,y=3){this.x=x;this.y=y;}}
function missingNumber():number{return undefined as unknown as number;}
function number(value:number):number {const point=new Point(value,value+1);return point.x+point.y;}
function nullable(value:number):number {const point=new Point(value,missingNumber());return point.x+point.y;}
console.log('numbers',number(11),nullable(12),nullable(missingNumber()));
function points():Point[]{return [new Point(),new Point(13),new Point(missingNumber(),17),new Point(19,23)];}
for(const point of points())console.log('point',point.x,point.y,Object.keys(point).join(','),point instanceof Point,Object.getPrototypeOf(point)===Point.prototype);
class Mixed {value=0;label='';enabled=false;constructor(value=7,label='default',enabled=true){this.value=value;this.label=label;this.enabled=enabled;}}
function missingText():string{return undefined as unknown as string;}
function missingBoolean():boolean{return undefined as unknown as boolean;}
function mixed(value:number,label:string,enabled:boolean):string{const item=new Mixed(value,label,enabled);return item.value+':'+item.label+':'+item.enabled;}
console.log('mixed',mixed(0,'',false),mixed(missingNumber(),missingText(),missingBoolean()));
console.log('null text',mixed(1,null as unknown as string,true));
const escaped=new Mixed(31,'retained',false);console.log('retained',escaped.value,escaped.label,escaped.enabled,Object.keys(escaped).join(','),escaped instanceof Mixed);
let trace='';
function argument(label:string,value:number):number{trace+=label;return value;}
const ordered=new Point(argument('x',missingNumber()),argument('y',29));console.log('order',trace,ordered.x,ordered.y);
trace='';function fail():number{trace+='throw';throw new Error('argument');}
try{new Point(argument('x',5),fail());}catch(error){console.log('throw',trace,(error as Error).message);}
class Effect {value=0;constructor(value=argument('default',37)){this.value=value;}}
trace='';console.log('effect supplied',new Effect(41).value,trace);console.log('effect missing',new Effect(missingNumber()).value,trace);
class Earlier {a=0;b=0;constructor(a=43,b=a+1){this.a=a;this.b=b;}}
console.log('earlier',new Earlier(47).a,new Earlier(47).b,new Earlier(missingNumber()).b);
class Signed {value=0;constructor(value=-0){this.value=value;}}
console.log('negative zero',Object.is(new Signed(missingNumber()).value,-0),Object.is(new Signed(0).value,-0));
class Parameters {constructor(public value=53,public enabled=true){}}
const parameter=new Parameters(missingNumber(),false);console.log('parameter fields',parameter.value,parameter.enabled,Object.keys(parameter).join(','));
function captured(seed:number):number{class Local {value=0;constructor(value=seed){this.value=value;}}return new Local(missingNumber()).value;}
console.log('captured',captured(59),captured(61));

function kernel(count:number):number{let total=0;for(let i=0;i<count;i++){const point=new Point((i&1)===0?missingNumber():i,i+1);total+=point.x+point.y;}return total;}
function escape():Point{return new Point(missingNumber(),17);}
console.log('kernel',kernel(10),escape().x,escape().y);

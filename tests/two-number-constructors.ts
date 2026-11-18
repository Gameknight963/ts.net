export {};
function adjust(value: number): number {return value + 0;}
const events: string[] = [];
function argument(value: number): number {events.push('arg:' + value);return value;}
class Pair {
    field = (events.push('field'), 1);
    sum: number;
    target: unknown;
    constructor(first: number, second: number) {events.push('body');this.sum=adjust(first)+second;this.target=new.target;}
}
const pair=new Pair(argument(2),argument(3));
console.log('pair', pair.sum, pair.target===Pair, events.join(','));
const Loose: any=Pair;
console.log('missing', String(new Loose(2).sum), String(new Loose().sum));
const boxed:unknown=2;
console.log('object argument',String(new Loose(boxed).sum));
class Zero {value=7;constructor(){events.push('zero');}}
const ZeroLoose:any=Zero;
console.log('zero parameters',new ZeroLoose(argument(10),argument(20)).value,events.slice(-3).join(','));
class One {sum:number;constructor(value:number){this.sum=adjust(value);}}
const OneLoose:any=One;
console.log('one parameter',new OneLoose(2,3).sum);
class Child extends Pair {extra=7;}
const child=new Child(4,5);
console.log('derived', child.sum, child.extra, child.target===Child);
class Replaced {sum=0;constructor(first:number,second:number){this.sum=adjust(first);return {sum:first+second} as any;}}
const replaced=new Replaced(6,7);
console.log('replacement',replaced.sum,replaced instanceof Replaced);
class Observed {sum:number;constructor(first:number,second:number){this.sum=arguments.length+arguments[0]+arguments[1];}}
console.log('arguments',new Observed(2,3).sum);
class CapturedArguments {read:()=>number;constructor(first:number,second:number){this.read=()=>arguments[1];}}
console.log('captured arguments',new CapturedArguments(2,3).read());
class Defaults {sum:number;constructor(first=2,second=3){this.sum=adjust(first)+second;}}
console.log('defaults',new Defaults(4,5).sum,new Defaults().sum,new Defaults(undefined,7).sum);
class Rest {sum:number;constructor(first:number,...rest:number[]){this.sum=adjust(first)+rest.length+rest[0];}}
console.log('rest',new Rest(2,3).sum);
function capture(offset:number) {return class Captured {sum:number;constructor(first:number,second:number){this.sum=adjust(first)+second+offset;}};}
const Captured=capture(10);
console.log('capture',new Captured(2,3).sum);
const Bound:any=Pair.bind(null,8);
console.log('bound',new Bound(9,10).sum);
const proxy=new Proxy(Pair,{construct(target,args,newTarget){events.push('proxy:'+args.length);return Reflect.construct(target,args,newTarget);}});
console.log('proxy',new proxy(2,4).sum,events[events.length-3]);
console.log('array',new Array(2,3).join(','));
const Arrow:any=(first:number,second:number)=>first+second;
try {new Arrow(1,2);} catch(error) {console.log('arrow',(error as Error).name);}
export function kernel(count:number,offset:number):number {
    class Value {sum:number;constructor(first:number,second:number){this.sum=adjust(first)+second;}}
    let total=offset;
    for(let i=0;i<count;i++)total+=new Value(i,0).sum;
    return total;
}
console.log('kernel',kernel(10,0));

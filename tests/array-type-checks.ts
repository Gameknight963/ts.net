export {};
function classify(items: readonly any[]): void {
    for(let i=-1;i<=items.length;i++)console.log(i, typeof items[i] === 'number', typeof items[i] === 'string', typeof items[i] != 'number', 'string' == typeof items[i]);
}
const dense: number[] = [0, NaN, Infinity, -0];
(dense as any)[-1] = 'negative';
classify(dense);
classify(Object.freeze([1,2,3]));
const optional: (number|undefined)[] = [1,undefined,3];classify(optional);
const holes = [1,,3];classify(holes);
let reads = 0;
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype,'1',{get(){reads++;return 'inherited';}});
Object.setPrototypeOf(holes,prototype);classify(holes);console.log('inherited reads',reads);
Object.defineProperty(dense,'0',{get(){reads++;return 'getter';},configurable:true});
classify(dense);console.log('own reads',reads);
const changing: number[] = [1,2,3];
function mutate(items: readonly any[]): void {
 for(let i=0;i<items.length;i++) {
  console.log('changing',typeof items[i] === 'number',typeof items[i] === 'string');
  if(i===0)Object.defineProperty(items,'2',{get(){return 'later';},configurable:true});
 }
}
mutate(changing);
const resized: number[] = [1,2,3];resized.length=1;resized.length=3;classify(resized);

function classifyAt(items: readonly any[], key: number): void {
 console.log('key', key, typeof items[key] === 'number', typeof items[key] === 'string');
}
const named: any = [1,2,3];
named[-1]='negative';named[0.5]='fractional';named[NaN]='nan';named[Infinity]='infinity';named[-Infinity]='negative infinity';named['undefined']='missing';
named[4294967294]=7;named[4294967295]='ordinary';named[4294967296]='larger';
for(const key of [0,-0,1,3,-1,0.5,NaN,Infinity,-Infinity,2147483647,2147483648,4294967294,4294967295,4294967296])classifyAt(named,key);
classifyAt(named,undefined as any);

let order = '';
const ordered: any[] = [0];
Object.defineProperty(ordered,'0',{get(){order+='G';return 'value';}});
function receiver(): any[] {order+='A';return ordered;}
function key(): number {order+='I';return 0;}
console.log('evaluation order',typeof receiver()[key()] === 'string',order);

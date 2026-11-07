function union(first: any, second: any): any { return (Set.prototype as any).union.call(first, second); }
function show(label: string, value: any): void { console.log(label, Array.from(value).join(','), value.size); }
show('sets', union(new Set([3,1,2]), new Set([2,4,3,5])));
show('map', union(new Set([1]), new Map([[2,'two'],[1,'one'],[3,'three']])));
show('self', (() => { const value = new Set([2,1]); return union(value,value); })());
const special = union(new Set([NaN,-0,null,undefined]), new Set([NaN,0,null,undefined]));
console.log('special', special.size, special.has(NaN), 1 / (Array.from(special) as any[])[1]);
const object = {value: 1};
console.log('identity', union(new Set([object]),new Set([object,{value:1}])).size);
let effects = '';
const receiver = new Set([1,2]);
const other = {
    get size(): any { effects += 'S'; return {valueOf(): number { effects += 'N'; return 3; }}; },
    get has(): any { effects += 'H'; return () => { throw new Error('has called'); }; },
    get keys(): any { effects += 'K'; return function (this: any): any {
        effects += this === other ? 'C' : 'wrong'; receiver.add(3);
        let position = 0;
        return {get next(): any { effects += 'G'; receiver.add(4); return function (): any {
            effects += 'X'; receiver.add(99);
            return position++ === 0 ? {value:5,done:false} : {done:true};
        }; }};
    }; },
};
show('mutation', union(receiver, other));
show('receiver', receiver);
console.log('order', effects);
function check(label: string, value: any): void {
    try { show(label, union(new Set([1]),value)); } catch (error) { console.log(label, (error as Error).name); }
}
for (const size of [NaN,undefined,-1,-Infinity,-0.5,0,1.9,Infinity,'2',null]) {
    check('size '+String(size), {size,has:()=>true,keys:()=>new Set([2,2,3]).keys()});
}
for (const value of [null,undefined,3,'text',[],{size:0},{size:0,has:3},{size:0,has:()=>true,keys:3},
    {size:0,has:()=>true,keys:()=>3}, {size:0,has:()=>true,keys:()=>({next:3})},
    {size:0,has:()=>true,keys:()=>({next:()=>3})}]) check('invalid', value);
let closes = 0;
const abrupt = {size:1,has:()=>true,keys:()=>({next(): any { throw new Error('next failed'); },return(): any { closes++; return {}; }})};
try { union(new Set(),abrupt); } catch(error) { console.log('abrupt', (error as Error).message, closes); }
const valueFailure = {size:1,has:()=>true,keys:()=>({next(): any { return {done:false,get value(): any { throw new Error('value failed'); }}; },return(): any { closes++; return {}; }})};
try { union(new Set(),valueFailure); } catch(error) { console.log('value failure', (error as Error).message, closes); }
let touched = 0;
try { union({}, {get size(): number { touched++; return 0; }}); } catch(error) { console.log('receiver check', (error as Error).name,touched); }
class Derived extends Set<number> {}
const derived = new Derived([1]);
(derived as any).add = () => { throw new Error('add called'); };
(derived as any).constructor = () => { throw new Error('constructor called'); };
const result = union(derived,new Set([2]));
console.log('plain result', result instanceof Set, result instanceof Derived, Object.getPrototypeOf(result) === Set.prototype);
const metadata = Object.getOwnPropertyDescriptor(Set.prototype,'union')!;
console.log('metadata', metadata.value.name, metadata.value.length, metadata.writable, metadata.enumerable, metadata.configurable);


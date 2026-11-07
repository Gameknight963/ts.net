function call(name: string, source: any, other: any): any { return (Set.prototype as any)[name].call(source,other); }
function display(name: string, value: any): void {
    console.log(name, typeof value==='boolean'?value:Array.from(value).join(','));
}
for(const name of ['symmetricDifference','isSubsetOf','isSupersetOf','isDisjointFrom']) {
    for(const first of [[],[1],[3,1,2]])for(const second of [[],[2],[2,1,3],[4,5,6,7]]) {
        display(name,call(name,new Set(first),new Set(second)));
        display(name+' map',call(name,new Set(first),new Map(second.map(value=>[value,0]))));
    }
    for(const size of [0,3,Infinity]) {
        let log='';let position=0;const values=[2,2,4,4];
        const other={get size():number {log+='S';return size;},get has():any {log+='H';return function(this:any,value:number):boolean {log+='h'+value;return this===other&&value===2;};},get keys():any {log+='K';return function(this:any):any {
            log+=this===other?'k':'wrong';return {next():any {log+='n';return position<values.length?{value:values[position++],done:false}:{done:true};},return():any {log+='r';return {};}};
        };}};
        display(name+' custom '+size,call(name,new Set([1,2,3]),other));console.log('order',log);
    }
    const source=new Set([1,2,3]);let once=true;
    const mutable={size:Infinity,has(value:number):boolean {if(once){once=false;source.delete(2);source.add(4);}return value!==3;},keys():any {let index=0;return {next():any {source.delete(2);source.add(4);return index<4?{value:[2,2,4,4][index++],done:false}:{done:true};}};}};
    display(name+' mutation',call(name,source,mutable));
    const special=call(name,new Set<any>([NaN,-0,null,undefined]),new Set<any>([NaN,0,null,undefined]));
    display(name+' equality',special);
    const desc=Object.getOwnPropertyDescriptor(Set.prototype,name)!;
    console.log('metadata',desc.value.name,desc.value.length,desc.writable,desc.enumerable,desc.configurable);
    for(const value of [null,{}, {size:-1}, {size:0,has:3}, {size:0,has:()=>true,keys:3}]) {
        try{call(name,new Set(),value);}catch(error){console.log('invalid',name,(error as Error).name);}
    }
}
for(const name of ['isSupersetOf','isDisjointFrom']) {
    for(const mode of ['normal','throw','primitive','missing','noncallable']) {
        let position=0;let returns=0;
        const iterator:any={next():any {position++;return {done:false,value:name==='isSupersetOf'?9:1};}};
        if(mode==='normal')iterator.return=()=>{returns++;return {};};
        if(mode==='throw')iterator.return=()=>{returns++;throw new Error('close failed');};
        if(mode==='primitive')iterator.return=()=>{returns++;return 3;};
        if(mode==='noncallable')iterator.return=3;
        const other={size:0,has:()=>false,keys:()=>iterator};
        try{console.log('early',name,mode,call(name,new Set([1]),other),position,returns);}
        catch(error){console.log('early error',name,mode,(error as Error).name,position,returns);}
    }
    let returns=0;
    const other={size:0,has:()=>false,keys:()=>({next():any {throw new Error('next failed');},return():any {returns++;return {};}})};
    try{call(name,new Set([1]),other);}catch(error){console.log('step error',name,(error as Error).message,returns);}
}
const duplicate={size:4,has:()=>true,keys():any {let i=0;return {next():any {return i<4?{value:[1,1,3,3][i++],done:false}:{done:true};}};}};
display('duplicate symmetric',call('symmetricDifference',new Set([1,2]),duplicate));

for(const name of ['isSupersetOf','isDisjointFrom']) {
    let closed=0;
    const iterator:any=new Set([name==='isSupersetOf'?9:1,7]).keys();
    iterator.return=function(this:any):any {console.log('native receiver',this===iterator);closed++;return {};};
    const other={size:0,has:()=>false,keys:()=>iterator};
    console.log('native close',name,call(name,new Set([1]),other),closed,iterator.next().value);
}
class DerivedSet extends Set<number> {}
const derived=new DerivedSet([1,2]);
(derived as any).constructor=()=>{throw new Error('constructor called');};
(derived as any).add=()=>{throw new Error('add called');};
const plain=call('symmetricDifference',derived,new Set([2,3]));
console.log('plain symmetric',plain instanceof Set,plain instanceof DerivedSet,Array.from(plain).join(','));

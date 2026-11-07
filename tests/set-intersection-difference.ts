function operation(name: string, source: any, other: any): any { return (Set.prototype as any)[name].call(source,other); }
function show(label: string, value: any): void { console.log(label, Array.from(value).join(','), value.size); }
for (const name of ['intersection','difference']) {
    for (const first of [[],[3,1],[3,1,2,4]]) for (const second of [[],[2,1],[2,1,3,5,6]]) {
        show(name,operation(name,new Set(first),new Set(second)));
        show(name+' map',operation(name,new Set(first),new Map(second.map(value=>[value,'v']))));
    }
    for (const reportedSize of [0,2,10]) {
        let effects = '';
        let position = 0;
        const values = [2,2,1,9];
        const other = {
            get size(): number { effects += 'S'; return reportedSize; },
            get has(): any { effects += 'H'; return function(this: any,key: number): any { effects += 'h'+key; return this===other && key!==3 ? 'yes' : ''; }; },
            get keys(): any { effects += 'K'; return function(this: any): any {
                effects += this===other ? 'k' : 'wrong';
                return {next(): any { return position<values.length ? {value:values[position++],done:false} : {done:true}; }};
            }; },
        };
        show(name+' custom '+reportedSize,operation(name,new Set([3,1,2]),other));
        console.log('effects',effects);
    }
    const source = new Set([1,2,3]);
    let firstCall = true;
    const mutable = {size:10,has(value: number): boolean {
        if(firstCall) {firstCall=false;source.delete(2);source.add(4);source.delete(1);source.add(1);}
        return value!==3;
    },keys(): any {throw new Error('keys called');}};
    show(name+' mutation',operation(name,source,mutable));
    show('source',source);
    const large = new Set<number>(); for(let i=0;i<200;i++)large.add(i);
    let steps = 0;
    const shrinking = {size:Infinity,has(value: number): boolean {
        steps++;
        if(value===0)for(let i=1;i<180;i++)large.delete(i);
        return value%2===0;
    },keys(): any {throw new Error('keys called');}};
    const shrunk=operation(name,large,shrinking);
    console.log(name+' compact',shrunk.size,steps,shrunk.has(0),shrunk.has(180),shrunk.has(199));
    const keysSource = new Set([1,2,3]);
    let index = 0;
    const keyMutation = {size:0,has(): boolean {throw new Error('has called');},keys(): any {
        keysSource.add(4);
        return {next(): any { keysSource.delete(2);keysSource.add(5);return index<3?{value:[2,4,5][index++],done:false}:{done:true};}};
    }};
    show(name+' keys mutation',operation(name,keysSource,keyMutation));
    const shared = {x:1};
    const special = operation(name,new Set<any>([NaN,-0,shared,null,undefined]),new Set<any>([NaN,0,shared,null,undefined]));
    console.log(name+' equality',special.size,special.has(NaN),special.has(shared),special.has(null));
    class Derived extends Set<number> {}
    const derived = new Derived([1,2]);
    (derived as any).has=()=>{throw new Error('receiver has called');};
    (derived as any).add=()=>{throw new Error('receiver add called');};
    const plain = operation(name,derived,new Set([2,3]));
    console.log(name+' plain',plain instanceof Set,plain instanceof Derived);
    const descriptor=Object.getOwnPropertyDescriptor(Set.prototype,name)!;
    console.log('metadata',descriptor.value.name,descriptor.value.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);
    for(const other of [null,{}, {size:-1}, {size:0,has:()=>true,keys:3}]) {
        try {operation(name,new Set(),other);}catch(error){console.log(name+' invalid',(error as Error).name);}
    }
    let closes=0;
    const badKeys={size:0,has:()=>true,keys:()=>({next():any {throw new Error('next failed');},return():any {closes++;return {};}})};
    try {operation(name,new Set([1]),badKeys);}catch(error){console.log(name+' next error',(error as Error).message,closes);}
    const badHas={size:Infinity,has():boolean {throw new Error('has failed');},keys():any {throw new Error('keys called');}};
    try {operation(name,new Set([1]),badHas);}catch(error){console.log(name+' has error',(error as Error).message);}
    let touched=0;
    try {operation(name,{}, {get size():number {touched++;return 0;}});}catch(error){console.log(name+' receiver',(error as Error).name,touched);}
}

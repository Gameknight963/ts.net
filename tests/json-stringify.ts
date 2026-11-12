function show(label:string,value:any,replacer?:any,space?:any):void {console.log(label,JSON.stringify(value,replacer,space));}
for(const value of [null,true,false,0,-0,1.25,1e21,1e-7,NaN,Infinity,-Infinity,undefined,()=>1,'','"\\\b\f\n\r\t\u0000\u001F','\uD800x\uDC00\uD83D\uDE00','\u2028\u2029',[],{},[1,undefined,()=>1,NaN],{b:2,a:1,missing:undefined,fn:()=>1}])show('value',value);
show('keys',JSON.parse('{"b":1,"2":2,"1":3,"__proto__":4}'));
for(const value of [Object(3),Object('text'),Object(true),new Map(),new Set(),new WeakMap()])show('boxed and collections',value);
const shared={x:1};show('shared',{first:shared,second:shared});
const cycle:any={};cycle.self=cycle;
try{JSON.stringify(cycle);}catch(error){console.log('cycle',(error as Error).name);}
show('removed cycle',cycle,(key:string,value:any)=>key==='self'?undefined:value);
const array:any[]=[1,,3];
(Array.prototype as any)[1]=2;show('sparse inherited',array);delete (Array.prototype as any)[1];show('sparse',array);
let effects='';
const value={get toJSON():any {effects+='G';return function(this:any,key:string):any {effects+='T'+key;return {a:1,b:2};};}};
show('toJSON',value,function(this:any,key:string,value:any):any {effects+='R'+key;return key==='b'?undefined:value;});console.log('order',effects);
show('function toJSON',Object.assign(()=>1,{toJSON:()=>3}));
const object:any={a:1,b:2,c:3};
show('mutation',object,function(this:any,key:string,value:any):any {if(key==='a'){delete this.b;this.d=4;this.c=9;}return value;});
const grows:any[]=[1,2];show('array mutation',grows,function(this:any,key:string,value:any):any {if(key==='0'){this.push(3);delete this[1];}return value;});
for(const list of [['b','a','b'],[2,'1',2],[],[Object('a'),Object(2),true,null,{}]])show('list',{a:1,b:2,'1':3,'2':4},list);
show('list inherited',Object.create({inherited:7},{hidden:{value:8}}),['hidden','inherited']);
show('list nested',{b:{b:1,a:2},a:3},['b','a']);
for(const gap of [0,2,2.9,-1,NaN,Infinity,'..','abcdefghijk',Object(2),Object('x'),true])show('gap',{a:[1,{},[]],b:2},undefined,gap);
const number:any=Object(3);number.valueOf=()=>7;show('number conversion',number);
const text:any=Object('x');text.toString=()=> 'custom';show('text conversion',text);
show('root replaced',undefined,(key:string,value:any)=>key===''?{a:1}:value);
show('omitted root',1,()=>undefined);
try{JSON.stringify({get bad():number {throw new Error('getter failed');}});}catch(error){console.log('getter error',(error as Error).message);}
try{JSON.stringify(1,()=>{throw new Error('replacer failed');});}catch(error){console.log('replacer error',(error as Error).message);}
try{JSON.stringify({toJSON(){throw new Error('toJSON failed');}});}catch(error){console.log('toJSON error',(error as Error).message);}
console.log('metadata',JSON.stringify.name,JSON.stringify.length,Object.keys(JSON).length);
let nested:any=0;for(let i=0;i<1000;i++)nested=[nested];const encoded=JSON.stringify(nested)!;console.log('deep',encoded.length,encoded[0],encoded[encoded.length-1]);
let hash=0;
for(let code=0;code<65536;code++){const text=JSON.stringify(String.fromCharCode(code))!;for(let i=0;i<text.length;i++)hash=(hash*31+text.charCodeAt(i))|0;}
console.log('all code units',hash);
const setupLog:string[]=[];
const list:any[]=[Object('a'),Object(2)];
(list[0] as any).toString=()=>{setupLog.push('list');return 'a';};
const gap:any=Object(2);gap.valueOf=()=>{setupLog.push('gap');return 2;};
const setup={toJSON():any {setupLog.push('toJSON');return {a:1,'2':2};}};
JSON.stringify(setup,list,gap);console.log('setup order',setupLog.join(','));
const cut=JSON.stringify({a:1},undefined,'123456789\uD83D\uDE00')!;
const cutCodes:number[]=[];for(let i=0;i<cut.length;i++)cutCodes.push(cut.charCodeAt(i));console.log('gap code units',cutCodes.join(','));
const descriptor=Object.getOwnPropertyDescriptor(JSON,'stringify')!;console.log('descriptor',descriptor.writable,descriptor.enumerable,descriptor.configurable);
const snapshot:any=JSON.parse('{"a":0,"b":2,"c":3}');
snapshot[Symbol('hidden')]=99;
snapshot.a={toJSON():number {
    delete snapshot.b;
    snapshot.d=4;
    Object.setPrototypeOf(snapshot,{b:7});
    Object.defineProperty(snapshot,'c',{get(){return 9;},enumerable:true,configurable:true});
    return 1;
}};
show('key snapshot with nested mutation',snapshot);
const ordered:any=JSON.parse('{"z":1,"10":2,"2":3,"a":4}');
ordered[Symbol('omitted')]=5;
show('dynamic key order and symbols',ordered);
class JsonRecord {first=1;second=2;}
show('class fields',new JsonRecord());
const changingRecords:any[]=JSON.parse('[{"a":1,"b":2},{"a":3,"b":4},{"a":5,"b":6}]');
changingRecords[0].a={toJSON():number {
    delete changingRecords[1].b;
    changingRecords[1].c=9;
    changingRecords[2][Symbol('hidden')]=10;
    return 1;
}};
show('different keys between records',changingRecords);
const escapedNames = [String.fromCharCode(0xd800), 'a"b', 'slash\\key', 'face' + String.fromCharCode(0xd83d, 0xde00), 'nul' + String.fromCharCode(0)];
const escapedRecord: any = JSON.parse('{}');
for (const name of escapedNames) escapedRecord[name] = name.length;
show('repeated escaped names', [escapedRecord, escapedRecord, escapedRecord]);
show('repeated names with indentation', [escapedRecord, escapedRecord, escapedRecord], undefined, 2);
const longName = String.fromCharCode(0xd800).repeat(129);
const longRecord: any = {}; longRecord[longName] = 1; longRecord.short = 2;
show('long escaped name', [longRecord, longRecord]);
for (const depth of [0, 1, 15, 16, 17, 40]) {
    const root: any = {value: 1};
    let leaf = root;
    for (let i = 0; i < depth; i++) {leaf.next = {value: i}; leaf = leaf.next;}
    show('nested shared values ' + depth, [root, root]);
    leaf.loop = root;
    try {JSON.stringify(root);} catch (error) {console.log('nested cycle', depth, (error as Error).name);}
    delete leaf.loop;
    show('shared after cycle ' + depth, [root, {value: 2}, root]);
}
{
    const values: any[] = JSON.parse('[0,2,3]');
    let calls = '';
    const item: any = {toJSON(this: unknown, key: string): string {
        calls += 'T' + key + (this === item ? '+' : '-');
        values.length = 1;
        return key;
    }};
    Object.defineProperty(values, '0', {get() {calls += 'G0'; return item;}, configurable: true, enumerable: true});
    const prototype = Object.create(Array.prototype);
    Object.defineProperty(prototype, '1', {get: function(this: unknown) {
        calls += this === values ? 'G1+' : 'G1-';
        return {toJSON(key: string): string {calls += 'T' + key; return key;}};
    }});
    Object.setPrototypeOf(values, prototype);
    show('array keys after shrinking', values);
    console.log('array getter order', calls);
    values.length = 3;
    calls = '';
    show('array replacer keys after shrinking', values, function(this: unknown, key: string, value: any): any {
        if (Array.isArray(this)) calls += 'R' + key;
        return value;
    });
    console.log('array replacer order', calls);
}
show('numeric array values', [-0, 1, NaN, Infinity, 1e-7, 1e21]);
{
    const packed = [-0, 1, NaN, Infinity, 1e-7, 1e21];
    show('nested numeric indentation', {values: packed, again: packed}, undefined, 2);
    show('numeric property list', [packed, []], ['values'], '..');
    show('numeric replacer', packed, (key: string, value: any) => key === '1' ? 9 : value);
    const indexed = [1, 2, 3];
    let reads = 0;
    Object.defineProperty(indexed, '1', {get() {reads++; return 7;}, configurable: true});
    show('numeric accessor', indexed);
    console.log('numeric accessor reads', reads);
    const missing = [1, 2, 3];
    delete missing[1];
    show('numeric holes', missing);
}
{
    const order: string[] = [];
    const list: any[] = [];
    Object.defineProperty(list, '0', {get() {order.push('list'); return 'field';}, configurable: true});
    const spacing: any = Object(2);
    spacing.valueOf = () => {order.push('space'); return 2;};
    console.log('scalar setup order', JSON.stringify(3, list, spacing), order.join(','));
    console.log('scalar replacer', JSON.stringify(3, (key, value) => key === '' ? value + 1 : value));
}

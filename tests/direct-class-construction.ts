class Entry {
    value=0; name="initial"; active=false;
    constructor(value:number,name:string,active:boolean){this.value=value;this.name=name;this.active=active;}
}
function show(value:Entry):void {
    console.log(value.value,value.name,value.active,Object.keys(value).join(","),Object.getPrototypeOf(value)===Entry.prototype,value instanceof Entry);
    console.log(Object.hasOwn(value,"value"),Object.hasOwn(value,"name"),Object.hasOwn(value,"active"));
}
show(new Entry(3,"three",true));
show(new Entry(undefined as unknown as number,undefined as unknown as string,undefined as unknown as boolean));
show(new Entry(4,null as unknown as string,false));
let effects="";
function number():number {effects+="N";return 5;}
function text():string {effects+="S";return "five";}
function boolean():boolean {effects+="B";return true;}
show(new Entry(number(),text(),boolean()));console.log("arguments",effects);
function failing():string {effects+="X";throw new Error("argument");}
effects="";try{new Entry(number(),failing(),boolean());}catch(error){console.log((error as Error).message,effects);}
class Empty {value=7;name="empty";active=true;}
const empty=new Empty();console.log(empty.value,empty.name,empty.active,Object.keys(empty).join(","),empty instanceof Empty);
class ParameterFields {constructor(public value:number,public name:string,public active:boolean){}}
const parameters=new ParameterFields(8,"parameters",true);console.log(parameters.value,parameters.name,parameters.active,Object.keys(parameters).join(","));
class Defaults {value=0;name="";constructor(value=9,name="default"){this.value=value;this.name=name;}}
const defaults=new Defaults();console.log(defaults.value,defaults.name,Object.keys(defaults).join(","));
const supplied=new Defaults(undefined,"explicit");console.log(supplied.value,supplied.name);
function local(value:number):any {class Local {value=0;constructor(value:number){this.value=value;}}return {item:new Local(value),type:Local};}
const first=local(10),second=local(11);console.log(first.item.value,second.item.value,first.item instanceof first.type,first.item instanceof second.type,second.item instanceof second.type);
class Repeated {value=0;constructor(first:number,second:number){this.value=first;this.value=second;}}
console.log(new Repeated(12,13).value);
const changed=new Entry(14,"changed",true);delete (changed as any).name;console.log(changed.name,Object.keys(changed).join(","));
Object.defineProperty(changed,"value",{get(){return 15;},configurable:true});console.log(changed.value);
Object.defineProperty(Entry.prototype,"name",{set(_value:string){throw new Error("inherited setter");},configurable:true});
show(new Entry(16,"own",false));delete (Entry.prototype as any).name;
function before(count:number):Entry[]{const values:Late[]=[];for(let i=0;i<count;i++)values.push(new Late(i));return values as any;}
console.log("empty before",before(0).length);try{before(1);}catch(error){console.log("tdz",(error as Error).name);}
class Late {value=0;constructor(value:number){this.value=value;}}
console.log((before(1)[0] as any).value);

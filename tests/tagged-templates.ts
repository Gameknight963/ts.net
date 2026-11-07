function tag(parts:TemplateStringsArray,...values:any[]):string {console.log("parts",parts.length,JSON.stringify(parts),JSON.stringify(parts.raw),Array.isArray(parts),Array.isArray(parts.raw));return parts.join("|")+":"+values.join("|");}
console.log(tag`hello`);console.log(tag`a${1}b${"x"}c`);console.log(tag`line\n${2}\tend`);
console.log(String.raw`line\n${3}\tend`);
let previous:any;function identity(parts:TemplateStringsArray,...values:any[]):TemplateStringsArray{return parts;}function same():void{const current=identity`same`;console.log("identity",previous===current);previous=current;}same();same();console.log("different",identity`same`===identity`same`);
const object={value:7,tag(parts:TemplateStringsArray,value:number):number{console.log("receiver",this.value,parts[0],parts.raw[0]);return this.value+value;}};console.log(object.tag`x${2}`);
const invalid=identity`\unicode`;console.log("invalid",invalid[0],invalid.raw[0]);
const frozen=identity`a${1}b`;for(const action of [()=>{(frozen as any)[0]="x";},()=>{(frozen.raw as any)[0]="x";},()=>{(frozen as any).extra=1;},()=>{(frozen as any).push("x");},()=>{delete (frozen as any)[0];}])try{action();}catch(error){console.log("frozen",(error as Error).name);}
const descriptor=Object.getOwnPropertyDescriptor(frozen,"raw")!;console.log("raw descriptor",descriptor.writable,descriptor.enumerable,descriptor.configurable,Object.keys(frozen).join("|"));console.log("length",Object.getOwnPropertyDescriptor(frozen,"length")?.writable);
let events="";function choose():typeof tag{events+="tag";return tag;}function value():number{events+="value";return 5;}console.log(choose()`a${value()}b`,events);
console.log("unicode",identity`\u00e9`[0].charCodeAt(0),identity`\ud800`[0].charCodeAt(0));
console.log(String.raw`first
second`);

export {};
const events:string[]=[];
function trace(value:string){events.push(value);return value;}
class Text {value=trace('field');target:any;constructor(value:string){events.push('constructor');this.value=value;this.target=new.target;}}
const text=new Text(trace('argument'));
console.log(events.join(','),text.value,text.target===Text);
function factory(prefix:string){return class Captured {value=prefix;constructor(value:string){this.value=prefix+value;}};}
const Captured=factory('prefix:');console.log(new Captured('text').value);
class Logical {value=false;constructor(value:boolean){this.value=value;}}
console.log(new Logical(true).value,new Logical(false).value);
class ObjectValue {value=0;input:object;constructor(value:{value:number}){this.input=value;this.value=++value.value;}}
const input={value:2};const object=new ObjectValue(input);console.log(object.value,input.value,object.input===input);
class ArrayValue {value:number[];constructor(value:number[]){this.value=value;}}
console.log(new ArrayValue([1,2,3]).value.join(','));
class Callback {value=0;constructor(value:()=>number){this.value=value();}}
console.log(new Callback(()=>42).value);
class Unknown {value:unknown;constructor(value:unknown){this.value=value;}}
console.log(new Unknown('unknown').value,new Unknown(null).value,new Unknown(undefined).value);
class Returned {value='initial';constructor(value:string){this.value=value;return {value:'replacement:'+value} as any;}}
console.log(new Returned('text').value);
class Throwing {value='initial';constructor(value:string){this.value=value;throw value;}}
try{new Throwing('failure');}catch(error){console.log('caught',error);}
class Arguments {value='';constructor(value:string){console.log('arguments',arguments.length,arguments[0]);arguments[0]='changed';this.value=value;}}
console.log(new Arguments('original').value);
class Default {value='';constructor(value:string='default'){this.value=value;}}
console.log(new Default(undefined).value);
class Rest {value='';constructor(...values:string[]){this.value=values.join(',');}}
console.log(new Rest('rest').value);
class Pattern {value='';constructor({value}:{value:string}){this.value=value;}}
console.log(new Pattern({value:'pattern'}).value);
class Private {#value='';constructor(value:string){this.#value=value;}read(){return this.#value;}}
console.log(new Private('private').read());
class Child extends Text {extra='child';}
console.log(new Child('derived').value);
const Bound=Text.bind(null);console.log(new Bound('bound').value);
class Ignored {value='initial';constructor(){this.value='ignored';}}
console.log(new (Ignored as any)('argument').value);
class ParameterProperty {constructor(public readonly value:string){}read(){return this.value;}}
console.log(new ParameterProperty('parameter property').read());
class Generic<T> {constructor(public value:T){}}
console.log(new Generic<string>('generic').value);
class Closure {read:()=>string;constructor(value:string){this.read=()=>value;}}
console.log(new Closure('closure').read());
class CapturedArguments {read:()=>unknown;constructor(value:string){this.read=()=>arguments[0];}}
console.log(new CapturedArguments('captured arguments').read());

export function work(count:number,offset:number):number {
 class Value {value='';constructor(value:string){this.value=value;}}
 const values:Value[]=[];
 for(let i=0;i<count;i++)values.push(new Value('item'));
 let sum=0;for(let i=0;i<values.length;i++)sum+=i+values[i].value.length-4;
 return sum;
}
console.log(work(10,0));

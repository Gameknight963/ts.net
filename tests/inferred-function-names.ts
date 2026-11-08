export {};
let assigned:()=>number;assigned=function(){return 1;};console.log("assignment",assigned.name);
let arrow:()=>number;arrow=()=>2;console.log("arrow",arrow.name);
let logical:any;logical ||= function(){};console.log("or",logical.name);
let nullish:any;nullish ??= ()=>0;console.log("nullish",nullish.name);
let truthy:any=true;truthy &&= class {};console.log("and class",truthy.name);
let wrapped:any;(wrapped)=((function(){}));console.log("parentheses",wrapped.name);
let asserted:any;asserted=((()=>0) as ()=>number);console.log("assertion",asserted.name);
let explicit:any;explicit=function original(){};console.log("explicit",explicit.name);
let cls:any;cls=class {static label=this.name;};console.log("class",cls.name,cls.label);
function defaults(callback:()=>number=()=>3,ctor:any=class {}):string{return callback.name+":"+ctor.name;}
console.log("parameters",defaults());
function destructured({callback=()=>4}: {callback?:()=>number}={}):string{return callback.name;}
console.log("parameter pattern",destructured());
const {callback=()=>4}: {callback?:()=>number}={};console.log("binding",callback.name);
const {value:renamed=function(){}}: {value?:()=>void}={};console.log("renamed",renamed.name);
const [element=()=>0]: (()=>number)[]=[];console.log("array binding",element.name);
const object:any={};object.member=function(){};object.other=()=>0;object.ctor=class {};
console.log("properties stay anonymous",object.member.name,object.other.name,object.ctor.name);
console.log("literal",({member:function(){}}).member.name);
console.log("conditional",(true?function(){}:function(){}).name);
function noop():void{}
console.log("comma",(noop(),function(){}).name);
class Holder {
 #callback=()=>0;
 #ctor=class {};
 names():string{return this.#callback.name+":"+this.#ctor.name;}
}
console.log("private",new Holder().names());

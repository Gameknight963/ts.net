import {value,arrow,ordinary} from "./module-this-helper.js";
export {};
console.log("top",this===undefined,value===undefined,arrow()===undefined,ordinary()===undefined);
const local=()=>this;console.log("arrow",local()===undefined);
// Ordinary functions still receive their own this, while module arrows are lexical.
const object={value:7,method(){return this.value;},arrow:()=>this};console.log("object",object.method(),object.arrow()===undefined);
const computed={[(this as any)===undefined?"x":"y"]:7};console.log("computed",computed.x);

import * as namespace from './optional-chaining-values.js';
function Empty():void {}
const Constructor:any=Empty;Constructor.prototype=namespace;
const instance=new Constructor();
console.log(instance.changing,instance.maybe(3),instance instanceof Constructor,instance instanceof Object,instance.__proto__);
function Replacement():object{return {value:11};}
const Returning:any=Replacement;Returning.prototype=namespace;
const returned=new Returning();console.log(returned.value,returned instanceof Returning,returned instanceof Object);

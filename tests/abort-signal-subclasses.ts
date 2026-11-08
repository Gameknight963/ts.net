export {};
class Signal extends AbortSignal {}
console.log("declaration",typeof Signal,Signal.prototype instanceof AbortSignal,Signal.prototype instanceof EventTarget);
console.log("static methods",Signal.abort===AbortSignal.abort,Signal.any===AbortSignal.any,Signal.timeout===AbortSignal.timeout);
for(const Constructor of [AbortSignal,Signal])try{new Constructor();console.log("constructed");}catch(error){console.log("illegal constructor",(error as Error).name);}
class Explicit extends AbortSignal {constructor(){super();}}
try{new Explicit();console.log("constructed explicit");}catch(error){console.log("explicit super",(error as Error).name);}
const existing=AbortSignal.abort("existing");
class Replacement extends AbortSignal {constructor(replace=true){if(replace)return existing;super();}}
const replaced=new Replacement();
console.log("replacement",replaced===existing,replaced.aborted,replaced.reason,replaced instanceof AbortSignal,replaced instanceof Replacement);
const created=Signal.abort("static");
console.log("static result",created.aborted,created.reason,created instanceof AbortSignal,created instanceof Signal);
const controller=new AbortController();
const combined=Signal.any([controller.signal]);controller.abort("combined");
console.log("combined",combined.aborted,combined.reason,combined instanceof AbortSignal,combined instanceof Signal);

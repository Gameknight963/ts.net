const push:any=Array.prototype.push,pop:any=Array.prototype.pop;
const source:any={0:"a",length:1};console.log(push.call(source,"b","c"),JSON.stringify(source));console.log(pop.call(source),JSON.stringify(source));console.log(push.call(source),JSON.stringify(source));
for(const object of [{},{length:0},{length:-1},{length:NaN},{0:"a",length:1.9},{length:3}]){console.log("pop",pop.call(object),JSON.stringify(object));console.log("push",push.call(object,"x"),JSON.stringify(object));}
const inherited:any=Object.create({0:"inherited"});inherited.length=1;console.log(pop.call(inherited),inherited.length,Object.hasOwn(inherited,"0"));
const wide:any={length:4294967296};console.log(push.call(wide,"x"),wide[4294967296],pop.call(wide),wide.length);
try{push.call({length:9007199254740991},"x");}catch(error){console.log("overflow",(error as Error).name);}
for(const method of [push,pop])for(const value of [null,undefined,"abc"]){try{method.call(value,"x");}catch(error){console.log("invalid",(error as Error).name);}}
function args(...values:any[]):void {console.log("args",push.call(arguments,"extra"),pop.call(arguments),arguments.length);}args(1,2);

const indexOf:any=Array.prototype.indexOf,includes:any=Array.prototype.includes;
const source:any={0:"a",2:NaN,3:undefined,length:5};
for(const value of ["a","b",NaN,undefined,null])for(const start of [undefined,-Infinity,-5,-1,0,1,3,10,Infinity,NaN])console.log(indexOf.call(source,value,start),includes.call(source,value,start));
const inherited:any=Object.create({1:"inherited"});inherited.length=3;console.log("inherited",indexOf.call(inherited,"inherited"),includes.call(inherited,"inherited"));
console.log("string",indexOf.call("abc","b"),includes.call("abc","bc"),includes.call("abc","b"));
function args(...values:any[]):void{console.log("args",indexOf.call(arguments,2),includes.call(arguments,3));}args(1,2,3);
const wide:any={4294967296:"wide",length:4294967297};console.log("wide",indexOf.call(wide,"wide",4294967296),includes.call(wide,"wide",4294967296));
let count=0;const start={valueOf(){count++;return 0;}};console.log("empty",indexOf.call({},1,start),includes.call({},1,start),count);
const changing:any={0:"a",length:2};const mutate={valueOf(){changing[1]="b";return 0;}};console.log("convert",indexOf.call(changing,"b",mutate));
for(const method of [indexOf,includes])for(const value of [null,undefined])try{method.call(value,1);}catch(error){console.log("invalid",(error as Error).name);}

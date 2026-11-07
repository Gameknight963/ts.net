const reverse:any=Array.prototype.reverse;
for(const object of [{0:"a",1:"b",2:"c",length:3},{0:"a",length:3},{2:"c",length:3},{1:"b",length:4},{length:0},{}, {0:"a",length:1.9}])console.log(reverse.call(object)===object,JSON.stringify(object));
const inherited:any=Object.create({0:"a"});inherited[2]="c";inherited.length=3;console.log("inherited",reverse.call(inherited)===inherited,inherited[0],inherited[2],Object.keys(inherited).join("|"));
function args(...values:any[]):void{console.log("args",reverse.call(arguments)===arguments,arguments[0],arguments[1],arguments.length);}args(1,2);
for(const value of [null,undefined,"abc"]){try{reverse.call(value);}catch(error){console.log("invalid",(error as Error).name);}}
const lone=reverse.call("a");console.log("boxed",typeof lone,String(lone));
let events="";const getter:any={length:2,get 0(){events+="low";return "a";},set 0(value){events+="setlow";},get 1(){events+="high";return "b";},set 1(value){events+="sethigh";}};reverse.call(getter);console.log("events",events);

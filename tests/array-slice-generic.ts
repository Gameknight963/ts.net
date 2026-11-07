const slice:any=Array.prototype.slice;
const object:any={0:"a",2:"c",length:4};for(const start of [undefined,-Infinity,-10,-1,0,1,10,Infinity,NaN])for(const end of [undefined,-1,0,3,Infinity,NaN]){const result=slice.call(object,start,end);console.log(start,end,result.length,Object.keys(result).join("|"),JSON.stringify(result));}
console.log("string",JSON.stringify(slice.call("abc",1)),JSON.stringify(slice.call("\ud83d\ude00",0)));
const inherited:any=Object.create({1:"b"});inherited[0]="a";inherited.length=3;console.log("inherited",JSON.stringify(slice.call(inherited)),Object.keys(slice.call(inherited)).join("|"));
function args(...values:any[]):void{console.log("args",JSON.stringify(slice.call(arguments,1)));}args(1,2,3);
console.log("empty",JSON.stringify(slice.call({})),JSON.stringify(slice.call(42)));
const wide:any={4294967296:"wide",length:4294967297};console.log("wide",JSON.stringify(slice.call(wide,-1)));
let events="";const changing:any={0:"a",length:2};const start={valueOf(){events+="start";changing[1]="b";return 0;}};const end={valueOf(){events+="end";return 2;}};console.log("conversion",JSON.stringify(slice.call(changing,start,end)),events);
for(const value of [null,undefined])try{slice.call(value);}catch(error){console.log("invalid",(error as Error).name);}

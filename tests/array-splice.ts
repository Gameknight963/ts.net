const values=[0,1,2,3];console.log(values.splice(1,2,8,9,10).join("|"),values.join("|"));console.log(values.splice(-2,1).join("|"),values.join("|"));console.log(values.splice(1).join("|"),values.join("|"));console.log((values.splice as any).call(values).length,values.join("|"));
for(const start of [-Infinity,-10,-1,0,2,100,Infinity,NaN])for(const count of [-1,0,1,100,Infinity,NaN]){const source=[0,1,2];console.log(start,count,source.splice(start,count,7).join("|"),source.join("|"));}
const sparse:any[]=[,1,,3,,];const deleted=sparse.splice(1,3,"a");console.log("sparse",deleted.length,Object.keys(deleted).join("|"),deleted.join("|"),sparse.length,Object.keys(sparse).join("|"),sparse.join("|"));
const splice:any=Array.prototype.splice;const object:any={0:"a",2:"c",length:3};console.log(JSON.stringify(splice.call(object,1,1,"x","y")),JSON.stringify(object));
const inherited:any=Object.create({1:"b"});inherited[0]="a";inherited[2]="c";inherited.length=3;console.log("inherited",JSON.stringify(splice.call(inherited,0,2)),JSON.stringify(inherited));
console.log("missing",JSON.stringify(splice.call({},0,1,"x")));
const explicit=[1,2];console.log(explicit.splice(0,undefined).length,explicit.join("|"));
try{splice.call({length:9007199254740991},0,0,1);}catch(error){console.log("overflow",(error as Error).name);}
console.log("metadata",splice.name,splice.length);

export {};
interface Value {value?:number;}
function readValue(record:Value):number|undefined {return record.value;}
const mutable={x:1,y:2,value:3};
mutable.x++;
const compact={value:4};
const mutableAlias:any=mutable;
const compactAlias:any=compact;
for(const value of [undefined,-0,NaN,17]){
 mutableAlias.value=value;
 compactAlias.value=value;
 const first=readValue(mutable),second=readValue(compact);
 console.log("reads",first===undefined,second===undefined,Object.is(first,-0),Object.is(second,-0),Number.isNaN(first),Number.isNaN(second));
 const mutableCopy={...mutable,label:"mutable"};
 const compactCopy={...compact,label:"compact"};
 console.log("copies",readValue(mutableCopy)===undefined,readValue(compactCopy)===undefined,JSON.stringify(mutableCopy),JSON.stringify(compactCopy));
 console.log("enumeration",Object.values(mutable).length,Object.values(compact).length,Object.keys(compact).join(","));
}
Object.assign(compact,{value:23});
console.log("assign",readValue(compact));

export {};
// This Node version lacks the methods. Its reference path uses captured
// intrinsics so overrides cannot change the algorithm being compared.
const mapHas=Map.prototype.has,mapGet=Map.prototype.get,mapSet=Map.prototype.set;
const weakHas=WeakMap.prototype.has,weakGet=WeakMap.prototype.get,weakSet=WeakMap.prototype.set;
function method(weak:boolean,computed:boolean):any {
 const prototype:any=weak?WeakMap.prototype:Map.prototype;
 const name=computed?"getOrInsertComputed":"getOrInsert";
 if(prototype[name])return prototype[name];
 const has:any=weak?weakHas:mapHas,get:any=weak?weakGet:mapGet,set:any=weak?weakSet:mapSet;
 return function(this:any,key:any,argument:any):any {
  has.call(this,key); // Validate the collection brand first.
  if(weak && (key===null || typeof key!=="object" && typeof key!=="function" && (typeof key!=="symbol" || Symbol.keyFor(key)!==undefined)))throw new TypeError();
  if(computed && typeof argument!=="function")throw new TypeError();
  if(!weak && key===0)key=0;
  if(has.call(this,key))return get.call(this,key);
  const value=computed?argument(key):argument;set.call(this,key,value);return value;
 };
}
for(const weak of [false,true]){
 const insert=method(weak,false),compute=method(weak,true);
 const collection:any=weak?new WeakMap():new Map(),key:any=weak?{}:"key";
 console.log("insert",weak,insert.call(collection,key,1),insert.call(collection,key,2),collection.get(key));
 const missing:any=weak?{}:"missing";let calls=0;
 console.log("computed",compute.call(collection,missing,function(this:any,received:any){calls++;console.log("callback",this===undefined,received===missing,arguments.length);return 3;}),compute.call(collection,missing,()=>{calls++;return 4;}),calls);
 const empty:any=weak?{}:"empty";collection.set(empty,undefined);
 console.log("undefined",insert.call(collection,empty,5),compute.call(collection,empty,()=>{throw new Error("unexpected");}),collection.has(empty));
 try{compute.call(collection,key,null);}catch(error){console.log("validate callback",weak,(error as Error).name);}
 const recursive:any=weak?{}:"recursive";
 console.log("mutate",compute.call(collection,recursive,()=>{collection.set(recursive,8);return 9;}),collection.get(recursive));
 const failure:any=weak?{}:"failure";
 try{compute.call(collection,failure,()=>{throw new Error("callback failed");});}catch(error){console.log("throw",(error as Error).message,collection.has(failure));}
 const inherited:any=weak?{}:"inherited";
 collection.has=()=>{throw new Error("has override");};collection.get=()=>{throw new Error("get override");};collection.set=()=>{throw new Error("set override");};
 console.log("intrinsics",insert.call(collection,inherited,7),compute.call(collection,inherited,()=>8));
 for(const receiver of [{},new Set(),new WeakSet(),weak?new Map():new WeakMap()])try{insert.call(receiver,key,1);}catch(error){console.log("brand",weak,(error as Error).name);}
}
const compute=method(false,true),insert=method(false,false),map=new Map();
console.log("zero",compute.call(map,-0,(key:number)=>1/key),1/(map.keys().next().value as number));
console.log("nan",insert.call(map,NaN,1),insert.call(map,NaN,2),map.size);
const order=new Map();compute.call(order,"x",()=>{order.set("other",1);order.set("x",2);return 3;});console.log("order",JSON.stringify(Array.from(order)));
const weakInsert=method(true,false),weakCompute=method(true,true),weakMap=new WeakMap();
const symbol=Symbol("key");console.log("symbol",weakInsert.call(weakMap,symbol,1),weakCompute.call(weakMap,symbol,()=>2));
for(const key of [undefined,null,1,"key",Symbol.for("registered")])try{weakCompute.call(weakMap,key,()=>{throw new Error("callback ran");});}catch(error){console.log("weak key",(error as Error).name);}

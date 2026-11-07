import data from "./json-module-data.json" with {type:"json"};
console.log(data.name,data.count,data.values.join("|"),Object.hasOwn(data,"__proto__"),Object.getPrototypeOf(data)===Object.prototype);
import * as ns from "./json-module-data.json" with {type:"json"};
import {config} from "./json-module-forward.js";
console.log("namespace",Object.keys(ns).join("|"),(ns as any).default===data,config===data);
async function main():Promise<void>{
 const loaded=await import("./json-module-data.json",{with:{type:"json"}});
 console.log("dynamic",loaded.default===data,Object.keys(loaded).join("|"));
 data.count=9;console.log("shared",loaded.default.count,config.count);
}


import array from "./json-module-array.json" with {type:"json"};
import scalar from "./json-module-scalar.json" with {type:"json"};
console.log("roots",JSON.stringify(array),scalar,data.nested.valid);
async function options():Promise<void>{
 for(const value of [null,1,{with:null},{with:1},{with:{type:1}},{with:{other:"value"}},{with:{type:"wrong"}}])try{await import("./json-module-data.json",value as any);console.log("unexpected");}catch(error){console.log("invalid options",(error as Error).name);}
 let events="";const settings={get with(){events+="W";return {get type(){events+="T";return "json";}};}};
 const promise=import("./json-module-data.json",settings);console.log("attributes",events);console.log("loaded",(await promise).default===data,events);
 try{await import("./json-module-forward.js",{with:{type:"json"}});}catch(error){console.log("type mismatch",(error as Error).name);}
}
main().then(options).catch(error=>console.log("failure",String(error)));

interface Config {name:string;count:number;nested:{valid:boolean};values:number[];}
const typed:Config=data;console.log("typed",typed.name,typed.count,typed.nested.valid,typed.values.join("|"));

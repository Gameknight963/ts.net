console.log("identity",globalThis.globalThis===globalThis);
console.log("objects",globalThis.Math===Math,globalThis.JSON===JSON,globalThis.Reflect===Reflect,globalThis.Intl===Intl);
console.log("constructors",globalThis.Array===Array,globalThis.Object===Object,globalThis.Promise===Promise,globalThis.Map===Map,globalThis.Error===Error,globalThis.Uint8Array===Uint8Array);
console.log("constants",globalThis.undefined===undefined,Number.isNaN(globalThis.NaN),globalThis.Infinity===Infinity);
console.log("functions",globalThis.parseInt("42",10),globalThis.isFinite(1),globalThis.encodeURIComponent("a b"));
const state=globalThis as any;
state.tsnetTestValue=41;
console.log("custom",state.tsnetTestValue,Reflect.has(globalThis,"tsnetTestValue"));
state.tsnetTestValue++;
state.tsnetTestMethod=function(this:unknown){return this===globalThis;};
console.log("receiver",state.tsnetTestMethod(),state.tsnetTestValue);
console.log("keys",Object.keys(globalThis).includes("tsnetTestValue"),Object.getOwnPropertyNames(globalThis).includes("Math"));
console.log("delete",delete state.tsnetTestValue,typeof state.tsnetTestValue);
delete state.tsnetTestMethod;

function shadow(globalThis:{value:number}) {return globalThis.value;}
console.log("shadow",shadow({value:9}));

console.log("construction",new globalThis.Map([["a",3]]).get("a"),new globalThis.Uint8Array([2,4]).join(","),new globalThis.TypeError("message").message);
console.log("symbols",globalThis.Symbol.for("global-test")===Symbol.for("global-test"));
class GlobalMap extends globalThis.Map<string,number> { marker=9; }
const map=new GlobalMap([["b",4]]);
console.log("base",map.get("b"),map.marker,map instanceof Map,map instanceof GlobalMap);

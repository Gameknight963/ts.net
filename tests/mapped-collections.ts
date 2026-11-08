import type {ImportedLookup, ImportedPartial} from "./mapped-collection-types";
export {};
type Lookup=Readonly<Map<string,number>>;
type OptionalLookup=Partial<Map<string,number>>;
type GetOnly=Pick<Map<string,number>,"get">;
type WithoutSet=Omit<Map<string,number>,"set">;
type Optional<T>={[K in keyof T]?:T[K]};
function read(value:Lookup):number{return value.get("x")??0;}
function maybe(value:OptionalLookup):number{return value.get?.("x")??-1;}
function get(value:GetOnly):number{return value.get("x")??0;}
function size(value:WithoutSet):number{return value.size;}
function forward<T>(value:Optional<T>):Optional<T>{return value;}
const map=new Map([["x",7]]);
console.log("mapped map",read(map),maybe(map),maybe({}),get(map),size(map),forward<Map<string,number>>(map)===map);
function unique(value:Readonly<Set<string>>):string{return [...value].join(",");}
console.log("mapped set",unique(new Set(["a","b"])));
class NamedMap extends Map<string,number>{label="named";}
function named(value:Readonly<NamedMap>):string{return value.label+":"+value.get("x");}
const subclass=new NamedMap([["x",9]]);console.log("mapped subclass",named(subclass));
function wrapper(value:{data:Map<string,number>}):number{return value.data.size;}
console.log("wrapper",wrapper({data:map}));
function plain(value:Readonly<{value:number}>):number{return value.value;}
console.log("plain",plain({value:11}));

type FrozenWrapper=Readonly<{data:Map<string,number>}>;
function wrapped(value:FrozenWrapper):number{return value.data.size;}
console.log("mapped wrapper",wrapped({data:map}));
type NestedLookup=Readonly<Lookup>;
function nested(value:NestedLookup):number{return value.size;}
console.log("nested mapped",nested(map));
type Flags<T>={[K in keyof T]:boolean};
function flag(value:Flags<GetOnly>):boolean{return value.get;}
console.log("transformed",flag({get:true}));

function imported(value:ImportedLookup):number{return value.get("x")??0;}
function importedPartial(value:ImportedPartial<Map<string,number>>):boolean{return value.has?.("x")??false;}
console.log("imported aliases",imported(map),importedPartial(map));

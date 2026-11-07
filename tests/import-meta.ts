import {metadata,current} from "./import-meta-helper.js";
export {};
const meta=import.meta;const arrow=()=>import.meta;
console.log("identity",meta===import.meta,meta===arrow(),metadata===current(),meta===metadata);
console.log("url",meta.url.startsWith("file:///"),meta.url.split("/").pop()?.replace(/\.ts$/,".js"),metadata.url.split("/").pop()?.replace(/\.ts$/,".js"));
console.log("prototype",Object.getPrototypeOf(meta)===null,Object.hasOwn(meta,"url"));
const descriptor=Object.getOwnPropertyDescriptor(meta,"url")!;console.log("descriptor",descriptor.writable,descriptor.enumerable,descriptor.configurable);
(meta as any).counter=7;console.log("mutable",(import.meta as any).counter);const saved=meta.url;meta.url="changed";console.log("changed",import.meta===meta,import.meta.url);meta.url=saved;

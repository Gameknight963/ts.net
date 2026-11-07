import {shared,write} from "./global-this-helper.js";
console.log("module",this===undefined,(()=>this)()===undefined,globalThis.globalThis===globalThis);
console.log("builtins",globalThis.Math===Math,globalThis.Error===Error);
write(); console.log("shared",shared===globalThis,(globalThis as any).tsnetSharedState);
delete (globalThis as any).tsnetSharedState;

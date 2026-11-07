import * as empty from "./object-integrity-empty.js";
import * as populated from "./object-integrity-exports.js";
for (const value of [empty,populated]) {
    console.log(Object.isExtensible(value),Object.isSealed(value),Object.isFrozen(value),Reflect.isExtensible(value));
    const tag=Object.getOwnPropertyDescriptor(value,Symbol.toStringTag)!;
    console.log(tag.value,tag.writable,tag.enumerable,tag.configurable);
}

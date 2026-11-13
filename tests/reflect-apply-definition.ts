export {};
const target={value:1};console.log(Reflect.apply(Object.freeze,null,[target])===target,Object.isFrozen(target));
const second={value:2};const argumentsList=[second];console.log(Reflect.apply(Object.preventExtensions,null,argumentsList)===second,Object.isExtensible(second));

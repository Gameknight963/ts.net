export {};
const target={value:1};const argumentsList:any=[target];
console.log(Object.freeze.apply(null,argumentsList)===target,Object.isFrozen(target));
const second={value:2};const spread:any=[second];console.log(Object.seal.apply(null,[...spread] as [typeof second])===second,Object.isSealed(second));

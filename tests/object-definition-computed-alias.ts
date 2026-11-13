export {};
const method:string='freeze';const operations:any={selected:(Object as any)[method]};
const freeze=operations.selected;const target={value:1};console.log(freeze(target)===target,Object.isFrozen(target));
const bound=freeze.bind(Object);const second={value:2};console.log(bound(second)===second,Object.isFrozen(second));

export {};
function choose(namespace:any,name:string):any {return namespace[name];}
const method=choose(Object,'freeze');const target={value:1};console.log(method(target)===target,Object.isFrozen(target));

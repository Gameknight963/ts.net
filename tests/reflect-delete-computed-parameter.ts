export {};
function erase(namespace:any,method:string,target:any):boolean{return namespace[method](target,'first');}
const target={first:1,second:2};
console.log(erase(Reflect,'deleteProperty',target),Object.hasOwn(target,'first'),Object.keys(target).join(','));

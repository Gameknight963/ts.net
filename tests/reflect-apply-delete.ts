export {};
const remove=Reflect.deleteProperty;
const target={first:1,second:2};
const args:[object,string]=[target,'first'];
console.log(Reflect.apply(remove,null,args),Object.hasOwn(target,'first'),Object.keys(target).join(','));

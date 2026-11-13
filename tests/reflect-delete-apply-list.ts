export {};
const target={first:1,second:2};
const args:[object,string]=[target,'first'];
console.log(Reflect.deleteProperty.apply(null,args),Object.hasOwn(target,'first'),Object.keys(target).join(','));

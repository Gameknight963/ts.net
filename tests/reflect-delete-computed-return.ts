export {};
function select(flag:boolean,method:string):any{return flag?(Reflect as any)[method]:Reflect.deleteProperty;}
const remove=select(true,'deleteProperty');
const target={first:1,second:2};
console.log(remove(target,'first'),Object.hasOwn(target,'first'),Object.keys(target).join(','));

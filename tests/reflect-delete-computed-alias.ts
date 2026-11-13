export {};
const method:string='deleteProperty';
const methods:any={remove:(Reflect as any)[method]};
const {remove}=methods;
const bound=remove.bind(null);
const target={first:1,second:2};
console.log(bound(target,'first'),Object.hasOwn(target,'first'),Object.keys(target).join(','));

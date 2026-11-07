const record={remove:1,keep:2};
const untouched={keep:3};
console.log('direct',Reflect.deleteProperty(record,'remove'),Object.hasOwn(record,'remove'),record.remove,record.keep,untouched.keep);
const indexed={remove:4};console.log('indexed',Reflect['deleteProperty'](indexed,'remove'),Object.keys(indexed).length);

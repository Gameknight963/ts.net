const value={dynamicOnly:1};
const key='dynamicOnly';
Reflect.deleteProperty(value,key);
console.log('dynamic key',Object.hasOwn(value,key),Object.keys(value).length);

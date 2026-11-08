const remove=Reflect.deleteProperty;
const value={fixedOnly:1};
remove(value,'fixedOnly');
console.log('method alias',Object.keys(value).length,Object.hasOwn(value,'fixedOnly'));

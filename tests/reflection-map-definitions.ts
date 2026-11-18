export {};
const methods=new Map([['define',Object.defineProperty]]);
const target={value:1};
methods.get('define')!(target,'value',{value:7,writable:false,enumerable:false,configurable:true});
console.log(target.value,Object.keys(target).length,Object.getOwnPropertyDescriptor(target,'value')!.writable);

const namespace:any=({}).constructor;
const saved=namespace.keys;
namespace.keys=()=>['changed'];
console.log(Object.keys({a:1})[0]);
namespace.keys=saved;
console.log(Object.keys({a:1})[0]);

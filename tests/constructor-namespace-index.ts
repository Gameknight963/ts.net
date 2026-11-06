const suffix='structor';const key='con'+suffix;
const namespace:any=({} as any)[key];
const saved=namespace.values;
namespace.values=()=>[17];
console.log(Object.values({a:1})[0]);
namespace.values=saved;
console.log(Object.values({a:1})[0]);

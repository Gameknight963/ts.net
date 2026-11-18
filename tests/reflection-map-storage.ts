export {};
const methods=new Map([['delete',Reflect.deleteProperty]]);
const namespaces=new Map([['reflect',Reflect]]);
const unused=methods.get('delete');
const namespace=namespaces.get('reflect');
function work():number {
 let sum=0;
 for(let i=0;i<100000;i++) {const point={x:i,y:i+1};sum+=point.x+point.y;}
 return sum;
}
console.log(unused===Reflect.deleteProperty,namespace===Reflect,work());

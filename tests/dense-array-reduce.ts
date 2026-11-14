export {};
function sumValues(sum:number,value:any):number{return sum+(typeof value==='number'?value:typeof value==='string'?value.length:0);}
let inheritedReads=0;const inherited:any[]=[1,,3];
const prototype=Object.create(Array.prototype);Object.defineProperty(prototype,'1',{get(){inheritedReads++;return 'four';}});Object.setPrototypeOf(inherited,prototype);
console.log('inherited',inherited.reduce(sumValues,0),inheritedReads);
console.log('undefined present',[1,undefined,,3].reduce(sumValues,0));
const sparse:any[]=[1];sparse[1000]='four';console.log('sparse',sparse.reduce(sumValues,0));
console.log('frozen',Object.freeze([1,'four',3]).reduce(sumValues,0));
for(const backwards of [false,true]) {
 const data:any[]=[1,'two',3];let calls=0,gets=0;
 const reducer=(sum:number,value:any):number=>{calls++;if(calls===1){delete data[1];const parent=Object.create(Array.prototype);Object.defineProperty(parent,'1',{get(){gets++;return 'changed';}});Object.setPrototypeOf(data,parent);}return sumValues(sum,value);};
 console.log('changed prototype',backwards,backwards?data.reduceRight(reducer,0):data.reduce(reducer,0),calls,gets);
}
const arrayLike:any={0:1,1:'four',2:3,length:3};console.log('array like',(Array.prototype.reduce as any).call(arrayLike,sumValues,0));

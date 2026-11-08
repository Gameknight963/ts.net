/// <reference lib="es2024.arraybuffer" />
export {};
const array=[10,20],typed=new Uint8Array([30,40]);
const arrayIterator=array.values(),typedIterator=typed.values();
const prototype:any=Object.getPrototypeOf(arrayIterator),next=prototype.next;
console.log('prototype',prototype===Object.getPrototypeOf(typedIterator),prototype===Object.getPrototypeOf(typed.keys()),prototype===Object.getPrototypeOf(typed.entries()));
console.log('borrowed',JSON.stringify(next.call(typedIterator)),JSON.stringify(typedIterator.next()),JSON.stringify(next.call(typedIterator)));
const typedNext=Object.getPrototypeOf(typed.values()).next;
console.log('reverse borrowed',JSON.stringify(typedNext.call(arrayIterator)),JSON.stringify(arrayIterator.next()));
console.log('tags',Object.prototype.toString.call(array.values()),Object.prototype.toString.call(typed.values()));
for(const value of [{},'abc'[Symbol.iterator](),null])try{next.call(value);}catch(error){console.log('brand',(error as Error).name);}
try {
 prototype.next=function(){return {value:'overridden',done:true};};
 console.log('override',JSON.stringify(typed.values().next()),JSON.stringify(array.values().next()));
}finally{prototype.next=next;}
const growable=new ArrayBuffer(2,{maxByteLength:4}),view=new Uint8Array(growable);view.set([1,2]);
const iterator=view.values();console.log('grow first',next.call(iterator).value);growable.resize(4);view[2]=3;view[3]=4;
console.log('grow rest',Array.from(iterator).join(','));

try {
 prototype.return=function(){console.log('closed',Object.prototype.toString.call(this));return {done:true};};
 for(const value of typed){console.log('typed break',value);break;}
 for(const value of array){console.log('array break',value);break;}
}finally{delete prototype.return;}

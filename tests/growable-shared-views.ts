export {};
function fresh(): [SharedArrayBuffer, Uint8Array] {
 const buffer=new SharedArrayBuffer(4,{maxByteLength:16});
 const array=new Uint8Array(buffer);array.set([1,2,3,4]);return [buffer,array];
}
{
 const [buffer,array]=fresh();const tracking=array.subarray(2),fixed=array.subarray(1,3);
 buffer.grow(8);array.set([5,6,7,8],4);
 console.log('subarray',tracking.length,Array.from(tracking).join(','),fixed.length,Array.from(fixed).join(','));
}
{
 const [buffer,array]=fresh();const seen:number[]=[];
 array.forEach((value,index)=>{seen.push(value);if(index===0){buffer.grow(8);array[4]=9;}});
 console.log('forEach',seen.join(','),array.length);
}
{
 const [buffer,array]=fresh();
 const mapped=array.map((value,index)=>{if(index===0){buffer.grow(8);array[4]=9;}return value*2;});
 console.log('map',mapped.length,Array.from(mapped).join(','),array.length);
}
{
 const [buffer,array]=fresh();const result=Array.from(array,(value,index)=>{if(index===0){buffer.grow(6);array[4]=5;array[5]=6;}return value;});
 console.log('from',result.join(','));
}
{
 const buffer=new SharedArrayBuffer(3,{maxByteLength:16});
 const array=new Uint16Array(buffer),view=new DataView(buffer,1);
 console.log('unaligned',array.length,array.byteLength,view.byteLength);
 buffer.grow(7);console.log('unaligned grown',array.length,array.byteLength,view.byteLength);
 const zero=new Uint8Array(buffer,7),fixed=new Uint8Array(buffer,7,0);buffer.grow(9);
 console.log('end views',zero.length,fixed.length);
}
{
 const [buffer,array]=fresh();const alias=structuredClone(buffer);
 const copied=new Uint8Array(array),sliced=array.slice();
 alias.grow(8);array[0]=99;
 console.log('copies',copied.length,sliced.length,copied[0],sliced[0]);
 const target=new Uint8Array(new SharedArrayBuffer(8,{maxByteLength:16}));
 target.set(array);console.log('set',Array.from(target).join(','));
}
{
 const [buffer,array]=fresh();
 const start={valueOf(){buffer.grow(8);array[4]=5;return 1;}};
 console.log('slice coercion',Array.from((array as any).slice(start)).join(','),array.length);
}

const sample='A\uD83D\uDE00\uD800Z\uDC00';
for(const index of [-Infinity,-4294967296,-1,-0.5,-0,0,1,1.9,2,3,4,5,6,4294967296,NaN,Infinity]) {
    console.log(index,sample.charCodeAt(index),sample.codePointAt(index),sample.charAt(index)===sample[index<0 ? -1 : index===index ? index|0 : 0]);
}
const dynamic: any=sample;
for(const index of [undefined,null,false,true,'2','bad',{valueOf:()=>3}]) {
    console.log(dynamic.charCodeAt(index),dynamic.codePointAt(index),dynamic.charAt(index).length);
}
console.log(dynamic.charAt(),dynamic.charCodeAt(),dynamic.codePointAt());
console.log(''.charAt(0),''.charCodeAt(0),''.codePointAt(0));
console.log(sample['charCodeAt'](2),sample['codePointAt'](1));
console.log(sample.charAt.name,sample.charCodeAt.name,sample.codePointAt.name,sample.charCodeAt.length);
console.log(sample.charAt==='x'.charAt,sample.charCodeAt==='x'.charCodeAt,sample.codePointAt==='x'.codePointAt);
const tuple: [number]=[1];
console.log(sample.charCodeAt(...tuple),sample.codePointAt(...tuple),sample.charAt(...tuple).length);
const spread: any[]=[1,2];
console.log(dynamic.charCodeAt(...spread),dynamic.codePointAt(...spread),dynamic.charAt(...spread).length);
let order='';
const index: any={valueOf:()=>{order+='I';return 1;}};
const borrowed={read:sample.charCodeAt,toString:()=>{order+='S';return 'abc';}};
console.log(borrowed.read(index),order);
order='';
const borrowedPoint={read:sample.codePointAt,toString:()=>{order+='S';return sample;}};
console.log((borrowedPoint.read as any)(index),order);
const borrowedChar={read:sample.charAt,toString:()=>sample};
console.log(borrowedChar.read(0),borrowedChar['read'](...tuple).length);
const numericReceiver: any={read:sample.charCodeAt,valueOf:()=>88,toString:()=>42};
console.log(numericReceiver.read(1));
const overridden: any={charCodeAt:(...args: any[])=>args.length};
console.log(overridden.charCodeAt(),overridden.charCodeAt(1),overridden.charCodeAt(...spread));
let evaluations=0;
console.log(dynamic.charCodeAt(0,++evaluations),evaluations);
console.log((sample.charCodeAt as any)(1),(sample.codePointAt as any)(2));

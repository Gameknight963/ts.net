const values: (number | undefined)[]=[10,20,-0,NaN,undefined];
const indices=[0,-0,1,-1,2,-2,4,-5,5,-6,1.8,-1.8,NaN,Infinity,-Infinity,4294967296];
for (const index of indices) console.log(index,values.at(index));
const dynamic: any=values;
for (const index of [undefined,null,false,true,'-1','0x2','invalid']) console.log(dynamic.at(index));
console.log(dynamic.at(),values['at'](-1));
const text='A\uD83D\uDE00Z';
for (let index=-6;index<=6;index++) {
    const position=index<0 ? text.length+index : index;
    console.log(index,text.at(index)===text[position]);
}
console.log('abc'.at(-1),'abc'.at(1.8),'abc'.at(NaN),''.at(0));
console.log(values.at === [1].at,values.at.name,values.at.length,values.at === ('x' as any).at);
let effects='';
const shrinking: any=[1,2,3];
const shrinkIndex: any={valueOf:()=>{effects+='I';shrinking.length=1;return -1;}};
console.log(shrinking.at(shrinkIndex),effects,shrinking.length);
effects='';
const growing: any=[];
const growIndex: any={valueOf:()=>{effects+='G';growing.push(9);return 0;}};
console.log(growing.at(growIndex),effects,growing.length);
const args: [number]=[-2];
console.log(values.at(...args),text.at(...args));
const wide: any[]=[-1,7];
console.log(dynamic.at(...wide));
const override: any=[1,2];
function change(): number {override.at=()=>99;override.push(3);return 7;}
console.log(override.at(-1,change()),override.at(-1));
override.at=(...args: any[])=>args.length;
console.log(override.at(),override.at(1),override.at(...wide));
const atSourceArray: any[]=[1];
const arrayMethod=atSourceArray.at;
const arraylike={read:arrayMethod,length:3,0:'first',2:'last'};
console.log(arraylike.read(-1),arraylike['read'](0));
const methodKey='read';console.log(arraylike[methodKey](1));
const stringMethod=''.at;
effects='';
const stringLike={read:stringMethod,toString:()=>{effects+='S';return 'xyz';}};
const stringIndex: any={valueOf:()=>{effects+='I';return -1;}};
console.log(stringLike.read(stringIndex),effects);
const borrowed: any={at:arrayMethod,length:'2',0:'a',1:'b'};
console.log(borrowed.at(-1));
const huge: number[]=[];huge.length=4294967295;huge[4294967294]=7;
console.log(huge.at(-1),huge.at(4294967294),huge.at(4294967295));

console.log((values.at as any)(-2),(text.at as any)(-1));
const numberLike={read:[1].at,length:1,0:7};
console.log(numberLike.read(0));
const moreArguments: [number]=[0];
console.log(arraylike.read(...moreArguments));
effects='';
const originalIndex: any={valueOf:(()=>0) as any};
const lengthValue: any={valueOf:()=>{effects+='L';originalIndex.valueOf=()=>{effects+='I';return 1;};return 2;}};
const ordered: any={at:arrayMethod,length:lengthValue,0:'a',1:'b'};
console.log(ordered.at(originalIndex),effects);

console.log((stringLike.read as any)(0));

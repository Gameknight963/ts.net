export {};
const codes=[0,8,9,10,11,12,13,14,32,0x85,0xa0,0x1680,0x180e,0x2000,0x200a,0x200b,0x2028,0x2029,0x202f,0x205f,0x2060,0x3000,0xfeff,0xffff];
for(const code of codes) {
 const character=String.fromCharCode(code);
 for(const text of [character+'1'+character,character]) {
  try{console.log(code,BigInt(text).toString());}catch(error){console.log(code,(error as Error).name);}
  console.log('compare',1n==(text as any),1n<(text as any),1n>=(text as any));
 }
}
for(const text of ['\ufeff \ufeff1\ufeff \ufeff',' \ufeff \ufeff0xff\ufeff \ufeff ','\ufeff\t\ufeff-123\ufeff\n\ufeff','\ufeff \ufeff','1\ufeff2','0x\ufeff1']) {
 try{console.log('mixed',JSON.stringify(text),BigInt(text).toString());}catch(error){console.log('mixed',JSON.stringify(text),(error as Error).name);}
}

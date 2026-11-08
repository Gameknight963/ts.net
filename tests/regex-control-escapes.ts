export {};
for(const suffix of ['A','B','Z','a','b','z','0','1','9','_','-','!','',']']) {
 for(const pattern of ['\\c'+suffix,'['+'\\c'+suffix+']']) {
  try {
   const regex=new RegExp(pattern);const matches:number[]=[];
   for(let code=0;code<128;code++) if(regex.test(String.fromCharCode(code))) matches.push(code);
   console.log(pattern,regex.source,matches.join(','),regex.test('\\c'+suffix));
  }catch(error){console.log(pattern,(error as Error).name);}
 }
}
console.log('quantifier',new RegExp('^\\c+$').test('\\ccc'),new RegExp('^\\cA+$').test('\x01\x01'));
console.log('replacement','a\x01b\x01'.replace(new RegExp('\\cA','g'),'x'));
console.log('escaped slash',new RegExp('\\\\cA').test('\\cA'));

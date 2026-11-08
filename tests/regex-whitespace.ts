export {};
const codes=[0,8,9,10,11,12,13,14,32,0x85,0xa0,0x1680,0x180e,0x2000,0x2001,0x200a,0x200b,0x2028,0x2029,0x202f,0x205f,0x2060,0x3000,0xfeff,0xffff];
for(const pattern of ['\\s','\\S','[\\s]','[\\S]','[^\\s]','[^\\S]','[x\\s]','[x\\S]','[\\s\\S]']) {
 const regex=new RegExp('^'+pattern+'$');console.log(pattern,codes.map(code=>regex.test(String.fromCharCode(code))).join(','));
}
console.log('split','a\u00a0b\ufeffc\u2028d'.split(/\s+/).join(','));
console.log('replace','a\u00a0b\ufeffc\u2028d'.replace(/\s/g,'_'));
for(const pattern of ['[a-\\s]','[\\s-z]','[a-\\S]','[\\S-z]','[\\s-\\S]','[a-\\d]','[\\d-z]','[a-\\w]','[\\W-z]']) {
 try{const regex=new RegExp('^'+pattern+'$');console.log('range',pattern,codes.concat([45,97,122]).map(code=>regex.test(String.fromCharCode(code))).join(','));}
 catch(error){console.log('range',pattern,(error as Error).name);}
}

const white=/^\s$/,nonwhite=/^\S$/,classWhite=/^[\s]$/,classNonwhite=/^[\S]$/;
const matching:number[]=[];let inconsistent=0;
for(let code=0;code<65536;code++) {
 const character=String.fromCharCode(code),match=white.test(character);
 if(match) matching.push(code);
 if(match!==classWhite.test(character)||match===nonwhite.test(character)||match===classNonwhite.test(character)) inconsistent++;
}
console.log('all code units',matching.join(','),inconsistent);

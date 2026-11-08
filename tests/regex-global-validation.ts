export {};
for(const matcher of [true,false,undefined,null]) {
 for(const flags of ['g','',undefined,null]) {
  let events='';
  const pattern:any={get [Symbol.match](){events+='match|';return matcher;},get flags(){events+='flags|';return flags;},
   [Symbol.replace](this:unknown,input:unknown,replacement:unknown){events+='replace|';return 'custom';},
   [Symbol.matchAll](this:unknown,input:unknown){events+='matchAll|';return ['custom'].values();}};
  try{console.log('replace',String(matcher),String(flags),'text'.replaceAll(pattern,'x'),events);}catch(error){console.log('replace',String(matcher),String(flags),(error as Error).name,events);}
  events='';try{console.log('all',String(matcher),String(flags),Array.from('text'.matchAll(pattern)).join(','),events);}catch(error){console.log('all',String(matcher),String(flags),(error as Error).name,events);}
 }
}
const native:any=/a/;native[Symbol.match]=false;
console.log('unmarked','aba'.replaceAll(native,'x'),Array.from('aba'.matchAll(native)).map(value=>value[0]).join(','));
let reads=0;const pattern:any={get [Symbol.match](){reads++;throw new Error('read');}};
for(const method of [String.prototype.replaceAll,String.prototype.matchAll])try{(method as any).call(null,pattern,'x');}catch(error){console.log('null receiver',(error as Error).name,reads);}

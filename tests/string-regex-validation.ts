export {};
for(const name of ['startsWith','endsWith','includes','indexOf','lastIndexOf']) {
 const method:any=(String.prototype as any)[name];
 for(const matcher of [true,false,undefined,null]) {
  let events='';
  const pattern:any={get [Symbol.match](){events+='match|';return matcher;},toString(){events+='needle|';return 'abc';}};
  const receiver:any={toString(){events+='receiver|';return 'abc';}};
  const position:any={valueOf(){events+='position|';return name==='endsWith'?3:0;}};
  try{console.log(name,String(matcher),method.call(receiver,pattern,position),events);}catch(error){console.log(name,String(matcher),(error as Error).name,events);}
 }
 const regex:any=/abc/;
 try{console.log('native',name,method.call('/abc/',regex));}catch(error){console.log('native',name,(error as Error).name);}
 regex[Symbol.match]=false;console.log('unmarked',name,method.call('/abc/',regex));
}
let reads=0;const invalid:any={get [Symbol.match](){reads++;throw new Error('lookup');}};
for(const name of ['startsWith','endsWith','includes']) {
 try{(String.prototype as any)[name].call(null,invalid);}catch(error){console.log('null',name,(error as Error).name,reads);}
}
const marked:any={[Symbol.match]:true,toString(){return 'abc';}};
try{console.log('direct','abc'.includes(marked));}catch(error){console.log('direct',(error as Error).name);}
const ordinary:any={[Symbol.match]:false,toString(){return 'abc';}};
console.log('direct ordinary','abc'.includes(ordinary));

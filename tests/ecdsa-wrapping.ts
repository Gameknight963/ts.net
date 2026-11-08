async function run():Promise<void>{
 const pair=await crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,['sign','verify']);
 const data=new TextEncoder().encode('wrapped ECDSA');
 const wrapping=await crypto.subtle.generateKey({name:'AES-KW',length:128},false,['wrapKey','unwrapKey']);
 for(const format of ['jwk','pkcs8'] as const){
  // PKCS#8 is not necessarily a multiple of eight bytes; use GCM for that format.
  const key=format==='jwk'?wrapping:await crypto.subtle.generateKey({name:'AES-GCM',length:128},false,['wrapKey','unwrapKey']);
  const algorithm=format==='jwk'?'AES-KW':{name:'AES-GCM',iv:new Uint8Array(12)};
  const wrapped=await crypto.subtle.wrapKey(format,pair.privateKey,key,algorithm);
  const restored=await crypto.subtle.unwrapKey(format,wrapped,key,algorithm,{name:'ECDSA',namedCurve:'P-256'},false,['sign']);
  const signature=await crypto.subtle.sign({name:'ECDSA',hash:'SHA-256'},restored,data);
  console.log('wrapped',format,restored.type,restored.extractable,await crypto.subtle.verify({name:'ECDSA',hash:'SHA-256'},pair.publicKey,signature,data));
 }
 const spki=await crypto.subtle.exportKey('spki',pair.publicKey);
 try{await crypto.subtle.importKey('spki',spki,{name:'ECDSA',namedCurve:'P-384'},true,['verify']);}catch(error){console.log('DER curve',(error as Error).name);}
 const invalid=new Uint8Array(spki);invalid[0]=0;
 try{await crypto.subtle.importKey('spki',invalid,{name:'ECDSA',namedCurve:'P-256'},true,['verify']);}catch(error){console.log('DER encoding',(error as Error).name);}
}
run();

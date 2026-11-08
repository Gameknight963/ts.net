async function run():Promise<void>{
 const data=new TextEncoder().encode('PSS test');
 for(const [hash,saltLength] of [['SHA-1',20],['SHA-256',32],['SHA-384',48],['SHA-512',64]] as const){
  const pair=await crypto.subtle.generateKey({name:'RSA-PSS',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash},true,['sign','verify']);
  const signing={name:'RSA-PSS',saltLength};
  const signature=await crypto.subtle.sign(signing,pair.privateKey,data);
  console.log('signature',hash,signature.byteLength,await crypto.subtle.verify(signing,pair.publicKey,signature,data));
  const changed=new Uint8Array(signature);changed[0]^=1;
  console.log('tampered',await crypto.subtle.verify(signing,pair.publicKey,changed,data));
  for(const format of ['jwk','spki'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.publicKey);
   const key=await crypto.subtle.importKey(format as any,exported as any,{name:'RSA-PSS',hash},true,['verify']);
   console.log('public',format,await crypto.subtle.verify(signing,key,signature,data));
  }
  for(const format of ['jwk','pkcs8'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.privateKey);
   const key=await crypto.subtle.importKey(format as any,exported as any,{name:'RSA-PSS',hash},true,['sign']);
   console.log('private',format,await crypto.subtle.verify(signing,pair.publicKey,await crypto.subtle.sign(signing,key,data),data));
  }
  const metadata=pair.publicKey.algorithm as RsaHashedKeyAlgorithm;
  const jwk=await crypto.subtle.exportKey('jwk',pair.publicKey);
  console.log('metadata',metadata.name,metadata.hash.name,jwk.alg);
  const cloned=structuredClone(pair);
  console.log('clone',await crypto.subtle.verify(signing,cloned.publicKey,await crypto.subtle.sign(signing,cloned.privateKey,data),data));
  let reads=0;
  await crypto.subtle.sign({name:'RSA-PSS',get saltLength(){reads++;return saltLength;}},pair.privateKey,data);
  console.log('getter',reads);
 }
}
run();

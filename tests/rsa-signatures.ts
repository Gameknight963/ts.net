async function main():Promise<void>{
 const data=new TextEncoder().encode('RSA signing test');
 for(const hash of ['SHA-1','SHA-256','SHA-384','SHA-512']){
  const algorithm={name:'RSASSA-PKCS1-v1_5',modulusLength:1024,publicExponent:new Uint8Array([1,0,1]),hash};
  const pair=await crypto.subtle.generateKey(algorithm,true,['sign','verify']);
  const metadata=pair.publicKey.algorithm as RsaHashedKeyAlgorithm;
  console.log('metadata',metadata.name,metadata.modulusLength,Array.from(metadata.publicExponent).join(','),metadata.hash.name,pair.privateKey.type,pair.publicKey.type);
  const signature=await crypto.subtle.sign('RSASSA-PKCS1-v1_5',pair.privateKey,data);
  console.log('signature',hash,signature.byteLength,await crypto.subtle.verify('RSASSA-PKCS1-v1_5',pair.publicKey,signature,data));
  const changed=new Uint8Array(signature);changed[0]^=1;
  console.log('tampered',await crypto.subtle.verify('RSASSA-PKCS1-v1_5',pair.publicKey,changed,data),await crypto.subtle.verify('RSASSA-PKCS1-v1_5',pair.publicKey,new Uint8Array(3),data));
  for(const format of ['spki','jwk'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.publicKey);
   const restored=await crypto.subtle.importKey(format as any,exported as any,{name:'RSASSA-PKCS1-v1_5',hash},true,['verify']);
   console.log('public',format,await crypto.subtle.verify('RSASSA-PKCS1-v1_5',restored,signature,data));
  }
  for(const format of ['pkcs8','jwk'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.privateKey);
   const restored=await crypto.subtle.importKey(format as any,exported as any,{name:'RSASSA-PKCS1-v1_5',hash},true,['sign']);
   console.log('private',format,Array.from(new Uint8Array(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',restored,data))).join(',')===Array.from(new Uint8Array(signature)).join(','));
  }
  const clone=structuredClone(pair);
  console.log('clone',await crypto.subtle.verify('RSASSA-PKCS1-v1_5',clone.publicKey,await crypto.subtle.sign('RSASSA-PKCS1-v1_5',clone.privateKey,data),data));
  const jwk=await crypto.subtle.exportKey('jwk',pair.publicKey);
  console.log('jwk',jwk.kty,jwk.alg,jwk.d===undefined,jwk.ext,jwk.key_ops?.join(','));
 }
}
main();

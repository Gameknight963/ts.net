async function run():Promise<void>{
 const algorithm={name:'RSASSA-PKCS1-v1_5',modulusLength:1024,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'};
 const pair=await crypto.subtle.generateKey(algorithm,true,['sign','verify']);
 const data=new Uint8Array([1,2]);
 async function failure(label:string,operation:()=>Promise<unknown>):Promise<void>{try{await operation();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
 await failure('public sign',()=>crypto.subtle.sign(algorithm,pair.publicKey,data));
 await failure('private verify',()=>crypto.subtle.verify(algorithm,pair.privateKey,new Uint8Array(128),data));
 await failure('empty usages',()=>crypto.subtle.generateKey(algorithm,true,[]));
 await failure('verify only',()=>crypto.subtle.generateKey(algorithm,true,['verify']));
 await failure('encrypt usage',()=>crypto.subtle.generateKey(algorithm,true,['encrypt']));
 await failure('hash',()=>crypto.subtle.generateKey({...algorithm,hash:'SHA-999'},true,['sign']));
 await failure('modulus',()=>crypto.subtle.generateKey({...algorithm,modulusLength:511},true,['sign']));
 const jwk=await crypto.subtle.exportKey('jwk',pair.privateKey);
 for(const change of [{kty:'EC'},{alg:'RS384'},{use:'enc'},{ext:false},{key_ops:['verify']},{key_ops:['sign','sign']}])
  await failure('jwk '+Object.keys(change)[0],()=>crypto.subtle.importKey('jwk',{...jwk,...change},algorithm,true,['sign']));
 await failure('raw export',()=>crypto.subtle.exportKey('raw',pair.publicKey));
 await failure('public pkcs8',()=>crypto.subtle.exportKey('pkcs8',pair.publicKey));
 await failure('private spki',()=>crypto.subtle.exportKey('spki',pair.privateKey));
 const publicJwk=await crypto.subtle.exportKey('jwk',pair.publicKey);
 console.log('empty public',(await crypto.subtle.importKey('jwk',publicJwk,algorithm,true,[])).usages.length);
 const limited=await crypto.subtle.generateKey(algorithm,false,['sign']);
 console.log('extraction',limited.privateKey.extractable,limited.publicKey.extractable,limited.publicKey.usages.length);
 await failure('nonextractable',()=>crypto.subtle.exportKey('pkcs8',limited.privateKey));
 const wrapping=await crypto.subtle.generateKey({name:'AES-GCM',length:128},false,['wrapKey','unwrapKey']);
 const options={name:'AES-GCM',iv:new Uint8Array(12)};
 for(const format of ['jwk','pkcs8'] as const){
  const wrapped=await crypto.subtle.wrapKey(format,pair.privateKey,wrapping,options);
  const restored=await crypto.subtle.unwrapKey(format,wrapped,wrapping,options,algorithm,false,['sign']);
  const signature=await crypto.subtle.sign(algorithm,restored,data);
  console.log('wrapped',format,await crypto.subtle.verify(algorithm,pair.publicKey,signature,data),restored.extractable);
 }
}
run();

async function run():Promise<void>{
 const algorithm={name:'RSA-OAEP',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'};
 const pair=await crypto.subtle.generateKey(algorithm,true,['encrypt','decrypt','wrapKey','unwrapKey']);
 const key=await crypto.subtle.generateKey({name:'AES-GCM',length:128},true,['encrypt','decrypt']);
 for(const format of ['raw','jwk'] as const){
  const wrapped=await crypto.subtle.wrapKey(format,key,pair.publicKey,'RSA-OAEP');
  const restored=await crypto.subtle.unwrapKey(format,wrapped,pair.privateKey,'RSA-OAEP',{name:'AES-GCM'},true,['encrypt','decrypt']);
  const original=await crypto.subtle.exportKey('raw',key),actual=await crypto.subtle.exportKey('raw',restored);
  console.log('wrapped',format,wrapped.byteLength,Array.from(new Uint8Array(original)).join(',')===Array.from(new Uint8Array(actual)).join(','));
 }
 async function failure(label:string,operation:()=>Promise<unknown>):Promise<void>{try{await operation();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
 await failure('public decrypt',()=>crypto.subtle.decrypt('RSA-OAEP',pair.publicKey,new Uint8Array(256)));
 await failure('private encrypt',()=>crypto.subtle.encrypt('RSA-OAEP',pair.privateKey,new Uint8Array(1)));
 await failure('too long',()=>crypto.subtle.encrypt('RSA-OAEP',pair.publicKey,new Uint8Array(191)));
 await failure('bad ciphertext',()=>crypto.subtle.decrypt('RSA-OAEP',pair.privateKey,new Uint8Array(3)));
 await failure('sign usage',()=>crypto.subtle.generateKey(algorithm,true,['sign']));
 await failure('empty usages',()=>crypto.subtle.generateKey(algorithm,true,[]));
 await failure('public only usages',()=>crypto.subtle.generateKey(algorithm,true,['encrypt']));
 const jwk=await crypto.subtle.exportKey('jwk',pair.privateKey);
 await failure('wrong use',()=>crypto.subtle.importKey('jwk',{...jwk,use:'sig'},algorithm,true,['decrypt']));
 await failure('wrong hash',()=>crypto.subtle.importKey('jwk',{...jwk,alg:'RSA-OAEP-384'},algorithm,true,['decrypt']));
 const onlyEncrypt=await crypto.subtle.importKey('spki',await crypto.subtle.exportKey('spki',pair.publicKey),algorithm,true,['encrypt']);
 await failure('wrap usage',()=>crypto.subtle.wrapKey('raw',key,onlyEncrypt,'RSA-OAEP'));
 let names=0,labels=0;
 await crypto.subtle.encrypt({get name(){names++;return 'RSA-OAEP';},get label(){labels++;return new Uint8Array(0);}},pair.publicKey,new Uint8Array(1));
 console.log('getters',names,labels);
}
run();

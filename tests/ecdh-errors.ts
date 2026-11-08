async function run():Promise<void>{
 const algorithm={name:'ECDH',namedCurve:'P-256'};
 const pair=await crypto.subtle.generateKey(algorithm,true,['deriveBits','deriveKey']);
 const peer=await crypto.subtle.generateKey(algorithm,true,['deriveBits']);
 async function failure(label:string,operation:()=>Promise<unknown>):Promise<void>{try{await operation();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
 await failure('empty usages',()=>crypto.subtle.generateKey(algorithm,true,[]));
 await failure('sign usage',()=>crypto.subtle.generateKey(algorithm,true,['sign']));
 await failure('public base',()=>crypto.subtle.deriveBits({name:'ECDH',public:peer.publicKey},pair.publicKey,128));
 await failure('private peer',()=>crypto.subtle.deriveBits({name:'ECDH',public:peer.privateKey},pair.privateKey,128));
 await failure('too long',()=>crypto.subtle.deriveBits({name:'ECDH',public:peer.publicKey},pair.privateKey,257));
 await failure('missing peer',()=>crypto.subtle.deriveBits({name:'ECDH'} as EcdhKeyDeriveParams,pair.privateKey,128));
 const wrongCurve=await crypto.subtle.generateKey({name:'ECDH',namedCurve:'P-384'},true,['deriveBits']);
 await failure('curve mismatch',()=>crypto.subtle.deriveBits({name:'ECDH',public:wrongCurve.publicKey},pair.privateKey,128));
 const signing=await crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,['sign']);
 await failure('algorithm mismatch',()=>crypto.subtle.deriveBits({name:'ECDH',public:signing.publicKey},pair.privateKey,128));
 const onlyKey=await crypto.subtle.generateKey(algorithm,true,['deriveKey']);
 await failure('usage mismatch',()=>crypto.subtle.deriveBits({name:'ECDH',public:peer.publicKey},onlyKey.privateKey,128));
 await failure('derived too long',()=>crypto.subtle.deriveKey({name:'ECDH',public:peer.publicKey},pair.privateKey,{name:'HMAC',hash:'SHA-512',length:1024},true,['sign']));
 const raw=await crypto.subtle.exportKey('raw',peer.publicKey);
 await failure('public usages',()=>crypto.subtle.importKey('raw',raw,algorithm,true,['deriveBits']));
 const jwk=await crypto.subtle.exportKey('jwk',pair.privateKey);
 await failure('JWK use',()=>crypto.subtle.importKey('jwk',{...jwk,use:'sig'},algorithm,true,['deriveBits']));
 const wrappedKey=await crypto.subtle.generateKey({name:'AES-GCM',length:128},false,['wrapKey','unwrapKey']);
 const wrapping={name:'AES-GCM',iv:new Uint8Array(12)};
 const wrapped=await crypto.subtle.wrapKey('pkcs8',pair.privateKey,wrappedKey,wrapping);
 const restored=await crypto.subtle.unwrapKey('pkcs8',wrapped,wrappedKey,wrapping,algorithm,false,['deriveBits']);
 const a=await crypto.subtle.deriveBits({name:'ECDH',public:peer.publicKey},restored,256);
 const b=await crypto.subtle.deriveBits({name:'ECDH',public:peer.publicKey},pair.privateKey,256);
 console.log('wrapped',Array.from(new Uint8Array(a)).join(',')===Array.from(new Uint8Array(b)).join(','),restored.extractable);
}
run();

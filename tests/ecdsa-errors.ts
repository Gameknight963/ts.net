async function run():Promise<void> {
 const pair=await crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,['sign','verify']);
 const data=new Uint8Array([1,2]);
 async function failure(label:string, operation:()=>Promise<unknown>):Promise<void> { try{await operation();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);} }
 await failure('private verify',()=>crypto.subtle.verify({name:'ECDSA',hash:'SHA-256'},pair.privateKey,new Uint8Array(64),data));
 await failure('public sign',()=>crypto.subtle.sign({name:'ECDSA',hash:'SHA-256'},pair.publicKey,data));
 await failure('unknown hash',()=>crypto.subtle.sign({name:'ECDSA',hash:'SHA-999'},pair.privateKey,data));
 await failure('unknown curve',()=>crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-999'},true,['sign']));
 await failure('empty usages',()=>crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,[]));
 await failure('verify only',()=>crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,['verify']));
 await failure('bad usage',()=>crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},true,['encrypt']));
 const raw=await crypto.subtle.exportKey('raw',pair.publicKey);
 await failure('wrong raw length',()=>crypto.subtle.importKey('raw',new Uint8Array(3),{name:'ECDSA',namedCurve:'P-256'},true,['verify']));
 await failure('wrong raw curve',()=>crypto.subtle.importKey('raw',raw,{name:'ECDSA',namedCurve:'P-384'},true,['verify']));
 await failure('public sign usage',()=>crypto.subtle.importKey('raw',raw,{name:'ECDSA',namedCurve:'P-256'},true,['sign']));
 const jwk=await crypto.subtle.exportKey('jwk',pair.privateKey);
 for(const change of [{crv:'P-384'},{kty:'oct'},{alg:'ES384'},{use:'enc'},{ext:false},{key_ops:['verify']},{key_ops:['sign','sign']},{x:'AA'},{d:'AA'},{d:'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA'}])
  await failure('jwk '+Object.keys(change)[0],()=>crypto.subtle.importKey('jwk',{...jwk,...change},{name:'ECDSA',namedCurve:'P-256'},true,['sign']));
 await failure('private raw export',()=>crypto.subtle.exportKey('raw',pair.privateKey));
 await failure('private spki export',()=>crypto.subtle.exportKey('spki',pair.privateKey));
 await failure('public pkcs8 export',()=>crypto.subtle.exportKey('pkcs8',pair.publicKey));
 const noExtract=await crypto.subtle.generateKey({name:'ECDSA',namedCurve:'P-256'},false,['sign']);
 console.log('extractable',noExtract.privateKey.extractable,noExtract.publicKey.extractable,noExtract.publicKey.usages.length);
 await failure('nonextractable',()=>crypto.subtle.exportKey('jwk',noExtract.privateKey));
 console.log('empty public usages',(await crypto.subtle.importKey('raw',raw,{name:'ECDSA',namedCurve:'P-256'},false,[])).usages.length);
 const publicOnly=await crypto.subtle.exportKey('jwk',pair.publicKey);
 console.log('public jwk',publicOnly.kty,publicOnly.crv,publicOnly.d===undefined,publicOnly.alg===undefined);
 let reads=0;
 await crypto.subtle.generateKey({name:'ECDSA',get namedCurve(){reads++;return 'P-256';}},true,['sign']);
 console.log('curve getter',reads);
}
run();

export {};
async function main():Promise<void>{
 const bytes=new Uint8Array(16),iv=new Uint8Array(12),data=new Uint8Array([1,2,3]);bytes.fill(7);
 const key=await crypto.subtle.importKey("raw",bytes,"AES-GCM",true,["encrypt","decrypt"]);
 const params={name:"AES-GCM",iv,additionalData:new Uint8Array([9])};
 const signature=await crypto.subtle.encrypt(params,key,data);
 const cloned=structuredClone({key,alias:key});
 console.log("clone",cloned.key!==key,cloned.key===cloned.alias,cloned.key.algorithm.name,(cloned.key.algorithm as AesKeyAlgorithm).length);
 console.log("clone decrypt",(new Uint8Array(await crypto.subtle.decrypt(params,cloned.key,signature)) as any).toHex());
 const pending=crypto.subtle.encrypt(params,key,data);data.fill(0);iv.fill(1);params.additionalData.fill(2);
 console.log("snapshot",(new Uint8Array(await pending) as any).toHex()===(new Uint8Array(signature) as any).toHex());
 const hmac=await crypto.subtle.importKey("raw",bytes,{name:"HMAC",hash:"SHA-256"},false,["sign"]);
 for(const [label,run] of [
  ["wrong key",()=>crypto.subtle.encrypt(params,hmac,data)],
  ["wrong signing key",()=>crypto.subtle.sign("HMAC",key,data)],
  ["short cipher",()=>crypto.subtle.decrypt(params,key,new Uint8Array(2))],
  ["wrong AAD",()=>crypto.subtle.decrypt({name:"AES-GCM",iv:new Uint8Array(12),additionalData:new Uint8Array([8])},key,signature)],
 ] as [string,()=>Promise<unknown>][]){try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}}
 const valid:JsonWebKey={kty:"oct",k:"AAAAAAAAAAAAAAAAAAAAAA",alg:"A128GCM",ext:true,key_ops:["encrypt","decrypt"]};
 for(const jwk of [{...valid,alg:"A256GCM"},{...valid,ext:false},{...valid,use:"sig"},{...valid,key_ops:["decrypt"]},{...valid,key_ops:["encrypt","encrypt"]}])try{await crypto.subtle.importKey("jwk",jwk,"AES-GCM",true,["encrypt"]);console.log("jwk accepted");}catch(error){console.log("jwk rejected",(error as Error).name);}
 (key.algorithm as AesKeyAlgorithm).name="HMAC";(key.algorithm as AesKeyAlgorithm).length=256;
 (key.usages as string[]).push("sign");
 const saved={name:"AES-GCM",iv:new Uint8Array(12),additionalData:new Uint8Array([9])};
 console.log("metadata isolation",(new Uint8Array(await crypto.subtle.decrypt(saved,key,signature)) as any).toHex());
 const locked=await crypto.subtle.importKey("raw",bytes,"AES-GCM",false,["encrypt"]);
 try{await crypto.subtle.exportKey("jwk",structuredClone(locked));console.log("clone export allowed");}catch(error){console.log("clone export",(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));

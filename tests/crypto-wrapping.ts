export {};
async function main():Promise<void>{
 const material=new Uint8Array(16);material.fill(7);
 const wrapping=await crypto.subtle.importKey("raw",material,"AES-GCM",false,["wrapKey","unwrapKey"]);
 const wrapOnly=await crypto.subtle.importKey("raw",material,"AES-GCM",false,["wrapKey"]);
 const unwrapOnly=await crypto.subtle.importKey("raw",material,"AES-GCM",false,["unwrapKey"]);
 const params={name:"AES-GCM",iv:new Uint8Array(12),additionalData:new Uint8Array([1,2,3])};
 const data=new Uint8Array([4,5,6]);
 for(const algorithm of [{name:"HMAC",hash:"SHA-256"},{name:"AES-GCM"}]){
  const hmac=algorithm.name==="HMAC",usages:KeyUsage[]=hmac?["sign","verify"]:["encrypt","decrypt"];
  const key=await crypto.subtle.importKey("raw",material,algorithm,true,usages);
  for(const format of ["raw","jwk"] as const){
   const wrapped=await crypto.subtle.wrapKey(format,key,wrapOnly,params);
   console.log("wrapped",algorithm.name,format,wrapped.byteLength,format==="raw"?(new Uint8Array(wrapped) as any).toHex():"jwk");
   const restored=await crypto.subtle.unwrapKey(format,wrapped,unwrapOnly,params,algorithm,false,usages);
   console.log("restored",restored instanceof CryptoKey,restored.type,restored.extractable,restored.algorithm.name,restored.usages.join(","));
   if(hmac)console.log("signature",await crypto.subtle.verify("HMAC",restored,await crypto.subtle.sign("HMAC",key,data),data));
   else console.log("decrypted",(new Uint8Array(await crypto.subtle.decrypt(params,restored,await crypto.subtle.encrypt(params,key,data))) as any).toHex());
   const corrupted=new Uint8Array(wrapped.slice(0));corrupted[0]^=1;
   try{await crypto.subtle.unwrapKey(format,corrupted,wrapping,params,algorithm,false,usages);console.log("tamper accepted");}catch(error){console.log("tamper",(error as Error).name);}
  }
 }
 const restricted=await crypto.subtle.importKey("raw",material,"AES-GCM",true,["encrypt","decrypt"]);
 const locked=await crypto.subtle.importKey("raw",material,"AES-GCM",false,["encrypt"]);
 const wrapped=await crypto.subtle.wrapKey("raw",restricted,wrapping,params);
 const failures:[string,()=>Promise<unknown>][]=[
  ["wrap permission",()=>crypto.subtle.wrapKey("raw",restricted,restricted,params)],
  ["unwrap permission",()=>crypto.subtle.unwrapKey("raw",wrapped,restricted,params,"AES-GCM",true,["encrypt"])],
  ["extraction",()=>crypto.subtle.wrapKey("raw",locked,wrapping,params)],
  ["encrypt permission",()=>crypto.subtle.encrypt(params,wrapping,data)],
  ["decrypt permission",()=>crypto.subtle.decrypt(params,wrapping,wrapped)],
 ];
 for(const [label,run] of failures)try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}
 for(const text of ["not json","null","{}",'{"kty":"oct","k":"AQID","ext":false,"key_ops":["sign"]}']){
  const ciphertext=await crypto.subtle.encrypt(params,restricted,new TextEncoder().encode(text));
  try{await crypto.subtle.unwrapKey("jwk",ciphertext,wrapping,params,{name:"HMAC",hash:"SHA-256"},true,["sign"]);console.log("invalid JWK accepted");}catch(error){console.log("invalid JWK",(error as Error).name);}
 }
 const encoded=new TextEncoder().encode('{"kty":"oct","k":"AQID","ext":false,"key_ops":["sign"]}');
 const ciphertext=await crypto.subtle.encrypt(params,restricted,encoded);
 const restored=await crypto.subtle.unwrapKey("jwk",ciphertext,unwrapOnly,params,{name:"HMAC",hash:"SHA-256"},false,["sign"]);
 console.log("nonextractable JWK",restored.extractable,restored.usages.join(","));
 const invalidUtf8=await crypto.subtle.encrypt(params,restricted,new Uint8Array([255]));
 try{await crypto.subtle.unwrapKey("jwk",invalidUtf8,unwrapOnly,params,"AES-GCM",false,["encrypt"]);console.log("UTF8 accepted");}catch(error){console.log("UTF8",(error as Error).name);}
 console.log("metadata",crypto.subtle.wrapKey.name,crypto.subtle.wrapKey.length,crypto.subtle.unwrapKey.name,crypto.subtle.unwrapKey.length);
}
main().catch(error=>console.log("failure",String(error)));

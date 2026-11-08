export {};
async function main():Promise<void>{
 function hex(text:string){return new Uint8Array(text.match(/../g)!.map(pair=>parseInt(pair,16)));}
 function text(bytes:ArrayBuffer):string{return Array.from(new Uint8Array(bytes)).map(x=>x.toString(16).padStart(2,"0")).join("");}
 const material=hex("00112233445566778899aabbccddeeff");
 const vectors=[
 ["000102030405060708090a0b0c0d0e0f","1fa68b0a8112b447aef34bd8fb5a7b829d3e862371d2cfe5"],
 ["000102030405060708090a0b0c0d0e0f1011121314151617","96778b25ae6ca435f92b5b97c050aed2468ab8a17ad84e5d"],
 ["000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f","64e8c3f9ce0f5ba263e9777905818a2a93c8191e7d6e8ae7"]
 ];
 const target=await crypto.subtle.importKey("raw",material,"AES-GCM",true,["encrypt"]);
 for(const [raw,expected] of vectors){
  const key=await crypto.subtle.importKey("raw",hex(raw),"AES-KW",true,["wrapKey","unwrapKey"]);
  console.log("key",key.algorithm.name,(key.algorithm as AesKeyAlgorithm).length,key.usages.join(","));
  const jwk=await crypto.subtle.exportKey("jwk",key);console.log("jwk",jwk.alg,jwk.kty,jwk.ext,jwk.key_ops!.join(","));
  const wrapped=await crypto.subtle.wrapKey("raw",target,key,"AES-KW");
  if(text(wrapped)!==expected)throw new Error("RFC vector");
  console.log("vector",raw.length*4,text(wrapped));
  const restored=await crypto.subtle.unwrapKey("raw",wrapped,key,"AES-KW","AES-GCM",true,["encrypt"]);
  console.log("restored",text(await crypto.subtle.exportKey("raw",restored)));
  const damaged=new Uint8Array(wrapped);damaged[0]^=1;
  try{await crypto.subtle.unwrapKey("raw",damaged,key,"AES-KW","AES-GCM",false,["encrypt"]);}catch(e){console.log("integrity",(e as Error).name);}
 }
 const keyForJwk=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-KW",true,["wrapKey","unwrapKey"]);
 const jwkWrapped=await crypto.subtle.wrapKey("jwk",target,keyForJwk,"AES-KW");
 const fromJwk=await crypto.subtle.unwrapKey("jwk",jwkWrapped,keyForJwk,"AES-KW","AES-GCM",true,["encrypt"]);
 console.log("jwk wrapped",jwkWrapped.byteLength % 8,text(await crypto.subtle.exportKey("raw",fromJwk)));
 const jwkKw=await crypto.subtle.exportKey("jwk",keyForJwk);
 const importedJwk=await crypto.subtle.importKey("jwk",jwkKw,"AES-KW",true,["wrapKey"]);
 console.log("jwk kw",text(await crypto.subtle.exportKey("raw",importedJwk)));
 const base=await crypto.subtle.importKey("raw",new Uint8Array([1,2,3]),"PBKDF2",false,["deriveKey"]);
 const derived=await crypto.subtle.deriveKey({name:"PBKDF2",hash:"SHA-256",salt:new Uint8Array([4,5]),iterations:10},base,{name:"AES-KW",length:128},true,["wrapKey"]);
 console.log("derived",text(await crypto.subtle.exportKey("raw",derived)));
 const generated=await crypto.subtle.generateKey({name:"AES-KW",length:256},true,["unwrapKey","wrapKey","wrapKey"]);
 console.log("generated",generated.type,generated.extractable,generated.algorithm.name,(generated.algorithm as AesKeyAlgorithm).length,generated.usages.join(","));
 const wrapping=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-KW",false,["wrapKey","unwrapKey"]);
 // Node reports DataError for empty wrapped input; this runtime reports OperationError.
 // Both reject the input. Other malformed lengths retain exact error-name checks.
 for(const length of [0,1,8,15,17,24,32]){
  if(length){const key=await crypto.subtle.importKey("raw",new Uint8Array(length),{name:"HMAC",hash:"SHA-256"},true,["sign"]);
   try{const wrapped=await crypto.subtle.wrapKey("raw",key,wrapping,"AES-KW");const restored=await crypto.subtle.unwrapKey("raw",wrapped,wrapping,"AES-KW",{name:"HMAC",hash:"SHA-256"},true,["sign"]);console.log("length",length,wrapped.byteLength,(await crypto.subtle.exportKey("raw",restored)).byteLength);}catch(e){console.log("length",length,(e as Error).name);}}
  try{await crypto.subtle.unwrapKey("raw",new Uint8Array(length),wrapping,"AES-KW","AES-GCM",false,["encrypt"]);}catch(e){console.log("bad wrapped",length,length === 0 ? ["DataError","OperationError"].includes((e as Error).name) : (e as Error).name);}
 }
 for(const usages of [["encrypt"],["decrypt"],[]])try{await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-KW",false,usages as KeyUsage[]);}catch(e){console.log("usages",usages.join(","),(e as Error).name);}
 for(const length of [128,192,256]){
  const key=await crypto.subtle.generateKey({name:"AES-KW",length},true,["wrapKey"]);
  console.log("generate length",length,(await crypto.subtle.exportKey("raw",key)).byteLength);
 }
 for(const length of [0,64,129,512])try{await crypto.subtle.generateKey({name:"AES-KW",length},true,["wrapKey"]);}catch(e){console.log("generate invalid",length,(e as Error).name);}
 for(const length of [0,15,17,31,33])try{await crypto.subtle.importKey("raw",new Uint8Array(length),"AES-KW",false,["wrapKey"]);}catch(e){console.log("import invalid",length,(e as Error).name);}
 const wrapOnly=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-KW",false,["wrapKey"]);
 const unwrapOnly=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-KW",false,["unwrapKey"]);
 const rawWrapped=await crypto.subtle.wrapKey("raw",target,wrapOnly,"AES-KW");
 try{await crypto.subtle.wrapKey("raw",target,unwrapOnly,"AES-KW");}catch(e){console.log("wrap usage",(e as Error).name);}
 try{await crypto.subtle.unwrapKey("raw",rawWrapped,wrapOnly,"AES-KW","AES-GCM",false,["encrypt"]);}catch(e){console.log("unwrap usage",(e as Error).name);}
 let corruptions=0;
 for(let index=0;index<rawWrapped.byteLength;index++){
  const damaged=new Uint8Array(rawWrapped.slice(0));damaged[index]^=128;
  try{await crypto.subtle.unwrapKey("raw",damaged,unwrapOnly,"AES-KW","AES-GCM",false,["encrypt"]);throw new Error("tamper accepted");}
  catch(e){if((e as Error).name!=="OperationError")throw e;corruptions++;}
 }
 console.log("corruptions",corruptions);
 for(const length of [16,24,32,64,128]){
  const bytes=new Uint8Array(length);for(let i=0;i<bytes.length;i++)bytes[i]=i*7;
  const key=await crypto.subtle.importKey("raw",bytes,{name:"HMAC",hash:"SHA-256"},true,["sign"]);
  const wrapped=await crypto.subtle.wrapKey("raw",key,wrapOnly,"AES-KW");
  const restored=await crypto.subtle.unwrapKey("raw",wrapped,unwrapOnly,"AES-KW",{name:"HMAC",hash:"SHA-256"},true,["sign"]);
  console.log("payload",length,text(wrapped),text(await crypto.subtle.exportKey("raw",restored))===text(bytes.buffer));
 }
 const privateTarget=await crypto.subtle.importKey("raw",material,"AES-GCM",false,["encrypt"]);
 try{await crypto.subtle.wrapKey("raw",privateTarget,wrapOnly,"AES-KW");}catch(e){console.log("not extractable",(e as Error).name);}
 for(const property of ["alg","use","key_ops","ext"]){
  const invalid:any={...jwkKw};invalid[property]=property==="alg"?"A128GCM":property==="use"?"sig":property==="key_ops"?["encrypt"]:false;
  try{await crypto.subtle.importKey("jwk",invalid,"AES-KW",true,["wrapKey"]);}catch(e){console.log("bad jwk",property,(e as Error).name);}
 }
 try{await crypto.subtle.encrypt("AES-KW",wrapping,material);}catch(e){console.log("encrypt unsupported",(e as Error).name);}
}
main().catch(e=>console.log("failure",String(e)));

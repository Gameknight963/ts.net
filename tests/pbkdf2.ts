export {};
async function main():Promise<void>{
 const password=new TextEncoder().encode("password"),salt=new TextEncoder().encode("salt"),data=new Uint8Array([1,2,3]);
 const key=await crypto.subtle.importKey("raw",password,"PBKDF2",false,["deriveBits","deriveKey"]);
 console.log("key",key instanceof CryptoKey,key.type,key.extractable,key.algorithm.name,(key.algorithm as any).length,key.usages.join(","));
 for(const hash of ["SHA-1","SHA-256","SHA-384","SHA-512"])for(const iterations of [1,2,4096])for(const length of [8,160,256,512]){
  const derived=await crypto.subtle.deriveBits({name:"PBKDF2",hash,salt,iterations},key,length);
  console.log("bits",hash,iterations,length,(new Uint8Array(derived) as any).toHex());
 }
 const parameters={name:"PBKDF2",hash:"SHA-256",salt,iterations:100};
 const keyOnly=await crypto.subtle.importKey("raw",password,"PBKDF2",false,["deriveKey"]);
 for(const length of [128,192,256]){
  const derived=await crypto.subtle.deriveKey(parameters,keyOnly,{name:"AES-GCM",length},true,["encrypt","decrypt"]);
  console.log("AES key",length,(new Uint8Array(await crypto.subtle.exportKey("raw",derived)) as any).toHex());
  const params={name:"AES-GCM",iv:new Uint8Array(12)};
  console.log("AES round trip",(new Uint8Array(await crypto.subtle.decrypt(params,derived,await crypto.subtle.encrypt(params,derived,data))) as any).toHex());
 }
 for(const hash of ["SHA-1","SHA-256","SHA-384","SHA-512"]){
  const derived=await crypto.subtle.deriveKey(parameters,keyOnly,{name:"HMAC",hash},true,["sign","verify"]);
  console.log("HMAC key",hash,(derived.algorithm as HmacKeyAlgorithm).length,(new Uint8Array(await crypto.subtle.exportKey("raw",derived)) as any).toHex());
  console.log("HMAC round trip",await crypto.subtle.verify("HMAC",derived,await crypto.subtle.sign("HMAC",derived,data),data));
 }
 const empty=await crypto.subtle.importKey("raw",new Uint8Array(),"PBKDF2",false,["deriveBits"]);
 console.log("empty",(new Uint8Array(await crypto.subtle.deriveBits({...parameters,salt:new Uint8Array()},empty,128)) as any).toHex());
 const cloned=structuredClone(key);console.log("clone",cloned!==key,cloned.algorithm.name,cloned.extractable,(new Uint8Array(await crypto.subtle.deriveBits(parameters,cloned,128)) as any).toHex());
 const failures:[string,()=>Promise<unknown>][]=[
  ["extractable",()=>crypto.subtle.importKey("raw",password,"PBKDF2",true,["deriveBits"])],
  ["usage",()=>crypto.subtle.importKey("raw",password,"PBKDF2",false,["sign"])],
  ["empty usages",()=>crypto.subtle.importKey("raw",password,"PBKDF2",false,[])],
  ["bits permission",()=>crypto.subtle.deriveBits(parameters,keyOnly,128)],
  ["zero iterations",()=>crypto.subtle.deriveBits({...parameters,iterations:0},key,128)],
  ["zero length",()=>crypto.subtle.deriveBits(parameters,key,0)],
  ["partial byte",()=>crypto.subtle.deriveBits(parameters,key,9)],
  ["hash",()=>crypto.subtle.deriveBits({...parameters,hash:"MD5"},key,128)],
  ["base export",()=>crypto.subtle.exportKey("raw",key)],
 ];
 for(const [label,run] of failures)try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}
 console.log("metadata",crypto.subtle.deriveBits.name,crypto.subtle.deriveBits.length,crypto.subtle.deriveKey.name,crypto.subtle.deriveKey.length);
}
main().catch(error=>console.log("failure",String(error)));

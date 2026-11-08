export {};
async function main():Promise<void>{
 const material=new Uint8Array(22);material.fill(11);
 const salt=new Uint8Array(13),info=new Uint8Array(10);for(let i=0;i<salt.length;i++)salt[i]=i;for(let i=0;i<info.length;i++)info[i]=240+i;
 const key=await crypto.subtle.importKey("raw",material,"HKDF",false,["deriveBits","deriveKey"]);
 console.log("key",key instanceof CryptoKey,key.type,key.extractable,key.algorithm.name,(key.algorithm as any).length,key.usages.join(","));
 const parameters={name:"HKDF",hash:"SHA-256",salt,info};
 console.log("RFC 5869",(new Uint8Array(await crypto.subtle.deriveBits(parameters,key,336)) as any).toHex());
 for(const hash of ["SHA-1","SHA-256","SHA-384","SHA-512"])for(const length of [0,8,160,256,1024])console.log("bits",hash,length,(new Uint8Array(await crypto.subtle.deriveBits({...parameters,hash},key,length)) as any).toHex());
 for(const [hash,bytes] of [["SHA-1",20],["SHA-256",32],["SHA-384",48],["SHA-512",64]] as const){
  const result=new Uint8Array(await crypto.subtle.deriveBits({...parameters,hash},key,255*bytes*8));
  console.log("maximum",hash,result.length,result[0],result[result.length-1]);
  try{await crypto.subtle.deriveBits({...parameters,hash},key,(255*bytes+1)*8);console.log("oversized accepted");}catch(error){console.log("oversized",hash,(error as Error).name);}
 }
 const derived=await crypto.subtle.deriveKey(parameters,key,{name:"AES-GCM",length:256},true,["encrypt","decrypt"]);
 console.log("AES",(new Uint8Array(await crypto.subtle.exportKey("raw",derived)) as any).toHex());
 const signed=await crypto.subtle.deriveKey(parameters,key,{name:"HMAC",hash:"SHA-256",length:256},true,["sign"]);
 console.log("HMAC",(new Uint8Array(await crypto.subtle.sign("HMAC",signed,new Uint8Array([1,2,3]))) as any).toHex());
 const empty=await crypto.subtle.importKey("raw",new Uint8Array(),"HKDF",false,["deriveBits"]);
 console.log("empty",(new Uint8Array(await crypto.subtle.deriveBits({...parameters,salt:new Uint8Array(),info:new Uint8Array()},empty,256)) as any).toHex());
 const clone=structuredClone(key);console.log("clone",clone!==key,clone.algorithm.name,(new Uint8Array(await crypto.subtle.deriveBits(parameters,clone,128)) as any).toHex());
 const wrong=await crypto.subtle.importKey("raw",material,"PBKDF2",false,["deriveBits"]);
 const failures:[string,()=>Promise<unknown>][]=[
  ["extractable",()=>crypto.subtle.importKey("raw",material,"HKDF",true,["deriveBits"])],
  ["usage",()=>crypto.subtle.importKey("raw",material,"HKDF",false,["encrypt"])],
  ["empty usages",()=>crypto.subtle.importKey("raw",material,"HKDF",false,[])],
  ["wrong algorithm",()=>crypto.subtle.deriveBits(parameters,wrong,128)],
  ["partial byte",()=>crypto.subtle.deriveBits(parameters,key,7)],
  ["missing info",()=>crypto.subtle.deriveBits({name:"HKDF",hash:"SHA-256",salt} as any,key,128)],
  ["hash",()=>crypto.subtle.deriveBits({...parameters,hash:"MD5"},key,128)],
  ["export",()=>crypto.subtle.exportKey("jwk",key)],
  ["key permission",()=>crypto.subtle.deriveKey(parameters,empty,{name:"AES-GCM",length:128},true,["encrypt"])],
 ];
 for(const [label,run] of failures)try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));

export {};
async function main():Promise<void>{
 const material=new Uint8Array([0,1,2,3,4,0]),salt=new Uint8Array([0,5,6,7,8,0]),info=new Uint8Array([9,10]);
 for(const name of ["PBKDF2","HKDF"]){
  const key=await crypto.subtle.importKey("raw",new DataView(material.buffer,1,4),name,false,["deriveBits","deriveKey"]);
  const parameters={name,hash:"SHA-256",salt:new DataView(salt.buffer,1,4),info,iterations:10};
  for(const length of [undefined,null,0,-0.5,1,7,8,8.9,9,16,-1,NaN,Infinity,4294967296])try{
   console.log("length",name,String(length),(new Uint8Array(await crypto.subtle.deriveBits(parameters,key,length as any)) as any).toHex());
  }catch(error){console.log("length error",name,String(length),(error as Error).name);}
  const pending=crypto.subtle.deriveBits(parameters,key,256);const result=new Uint8Array(await pending);
  const expected=(result as any).toHex();result.fill(0);
  console.log("output isolation",name,(new Uint8Array(await crypto.subtle.deriveBits(parameters,key,256)) as any).toHex()===expected);
  const derived=await crypto.subtle.deriveKey(parameters,key,{name:"HMAC",hash:"SHA-256",length:248},false,["sign"]);
  console.log("HMAC length",name,(derived.algorithm as HmacKeyAlgorithm).length,derived.extractable,(new Uint8Array(await crypto.subtle.sign("HMAC",derived,new Uint8Array([1]))) as any).toHex());
  for(const target of [{name:"HMAC",hash:"SHA-256",length:0},{name:"AES-GCM",length:127},{name:"AES-GCM"},{name:"PBKDF2"}])try{await crypto.subtle.deriveKey(parameters,key,target,true,["sign"]);console.log("target accepted");}catch(error){console.log("target",name,JSON.stringify(target),(error as Error).name);}
 }
 const key=await crypto.subtle.importKey("raw",material,"PBKDF2",false,["deriveBits"]);
 for(const iterations of [0,-0.5,-1,NaN,Infinity,1.9])try{console.log("iterations",String(iterations),(new Uint8Array(await crypto.subtle.deriveBits({name:"PBKDF2",hash:"SHA-256",salt,iterations},key,128)) as any).toHex());}catch(error){console.log("iteration error",String(iterations),(error as Error).name);}
 const seed=new Uint8Array(3000);seed.fill(5);const large=await crypto.subtle.importKey("raw",seed,"HKDF",false,["deriveBits"]);
 console.log("long key",(new Uint8Array(await crypto.subtle.deriveBits({name:"HKDF",hash:"SHA-256",salt,info},large,256)) as any).toHex());
}
main().catch(error=>console.log("failure",String(error)));

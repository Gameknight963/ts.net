export {};
async function main():Promise<void>{
 const bytes=new Uint8Array([1,2,3]),data=new Uint8Array([4,5,6]);
 for(const bits of [0,1,7,8,9,23,24,25])for(const op of ["generate","import"])try{
  const algorithm={name:"HMAC",hash:"SHA-256",length:bits};
  const key=op==="generate"?await crypto.subtle.generateKey(algorithm,true,["sign","sign"]):await crypto.subtle.importKey("raw",bytes,algorithm,true,["sign","sign"]);
  console.log(op,bits,(key.algorithm as HmacKeyAlgorithm).length,key.usages.join(","));
 }catch(error){console.log(op,bits,(error as Error).name);}
 const key=await crypto.subtle.importKey("raw",bytes,{name:"HMAC",hash:"SHA-256"},true,["sign"]);
 const signature=await crypto.subtle.sign("HMAC",key,data);
 console.log("identity",key.algorithm===key.algorithm,key.usages===key.usages);
 const publicAlgorithm=key.algorithm as HmacKeyAlgorithm;publicAlgorithm.hash.name="SHA-1";publicAlgorithm.length=800;
 (key.usages as string[]).push("verify");
 console.log("metadata isolation",(new Uint8Array(await crypto.subtle.sign("HMAC",key,data)) as any).toHex()===(new Uint8Array(signature) as any).toHex());
 try{await crypto.subtle.verify("HMAC",key,signature,data);console.log("permission changed");}catch(error){console.log("metadata permission",(error as Error).name);}
 const cloned=structuredClone({key,alias:key});
 console.log("clone",cloned.key!==key,cloned.key===cloned.alias,cloned.key instanceof CryptoKey,(cloned.key.algorithm as HmacKeyAlgorithm).hash.name,cloned.key.usages.join(","));
 console.log("clone signing",(new Uint8Array(await crypto.subtle.sign("HMAC",cloned.key,data)) as any).toHex()===(new Uint8Array(signature) as any).toHex());
 const exported=new Uint8Array(await crypto.subtle.exportKey("raw",key));exported.fill(0);
 console.log("export isolation",(new Uint8Array(await crypto.subtle.exportKey("raw",key)) as any).toHex());
 const pending=crypto.subtle.sign("HMAC",key,data);data.fill(0);console.log("input snapshot",(new Uint8Array(await pending) as any).toHex()===(new Uint8Array(signature) as any).toHex());
 let reads=0;const algorithm={name:"HMAC",get hash(){reads++;return "SHA-256";}};
 await crypto.subtle.importKey("raw",bytes,algorithm,false,["sign"]);console.log("hash getter",reads);
 const buffer=new ArrayBuffer(8);buffer.transfer();try{await crypto.subtle.importKey("raw",buffer,{name:"HMAC",hash:"SHA-256"},true,["sign"]);}catch(error){console.log("detached rejected",error instanceof Error);}
}
main().catch(error=>console.log("failure",String(error)));

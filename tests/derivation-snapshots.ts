export {};
async function main():Promise<void>{
 for(const name of ["PBKDF2","HKDF"]){
  const material=new Uint8Array([1,2,3]),salt=new Uint8Array([4,5,6]),info=new Uint8Array([7,8,9]);
  const importing=crypto.subtle.importKey("raw",material,name,false,["deriveBits","deriveKey"]);material.fill(0);
  const key=await importing,params={name,hash:"SHA-256",salt,info,iterations:100};
  const deriving=crypto.subtle.deriveBits(params,key,256);salt.fill(0);info.fill(0);
  const result=await deriving;
  const original={name,hash:"SHA-256",salt:new Uint8Array([4,5,6]),info:new Uint8Array([7,8,9]),iterations:100};
  console.log("snapshots",name,(new Uint8Array(result) as any).toHex(),(new Uint8Array(await crypto.subtle.deriveBits(original,key,256)) as any).toHex());
  key.algorithm.name="HMAC";(key.usages as string[]).push("sign");
  try{await crypto.subtle.sign("HMAC",key,new Uint8Array([1]));console.log("permission changed");}catch(error){console.log("permission",name,(error as Error).name);}
  const copy=structuredClone(key);
  console.log("cloned metadata",name,copy.algorithm.name,copy.usages.join(","));
  console.log("cloned bytes",name,(new Uint8Array(await crypto.subtle.deriveBits(original,copy,256)) as any).toHex());
 }
}
main().catch(error=>console.log("failure",String(error)));

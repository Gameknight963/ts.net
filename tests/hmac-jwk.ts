export {};
async function main():Promise<void>{
 const data=new Uint8Array([1,2,3]);
 for(const hash of ["SHA-1","SHA-256","SHA-384","SHA-512"]){
  const original=await crypto.subtle.importKey("raw",new Uint8Array([251,255,1,2,3]),{name:"HMAC",hash},true,["sign","verify"]);
  const jwk=await crypto.subtle.exportKey("jwk",original);
  console.log("jwk",hash,jwk.kty,jwk.k,jwk.alg,jwk.ext,jwk.key_ops?.join(","));
  const key=await crypto.subtle.importKey("jwk",jwk,{name:"HMAC",hash},false,["verify"]);
  console.log("round trip",await crypto.subtle.verify("HMAC",key,await crypto.subtle.sign("HMAC",original,data),data));
 }
 const valid:JsonWebKey={kty:"oct",k:"AQID",alg:"HS256",ext:true,key_ops:["sign","verify"]};
 const invalid:JsonWebKey[]=[{...valid,kty:"RSA"},{...valid,k:undefined},{...valid,k:""},{...valid,k:"a"},{...valid,k:"!!!"},{...valid,alg:"HS512"},{...valid,use:"enc"},{...valid,ext:false},{...valid,key_ops:["verify"]},{...valid,key_ops:["sign","sign"]}];
 for(let index=0;index<invalid.length;index++)try{await crypto.subtle.importKey("jwk",invalid[index],{name:"HMAC",hash:"SHA-256"},true,["sign"]);console.log("invalid",index,"unexpected success");}catch(error){console.log("invalid",index,(error as Error).name);}
 const key=await crypto.subtle.importKey("jwk",{kty:"oct",k:"AQID"},{name:"HMAC",hash:"SHA-256"},false,["sign"]);
 console.log("minimal",key.type,key.usages.join(","));
}
main().catch(error=>console.log("failure",String(error)));

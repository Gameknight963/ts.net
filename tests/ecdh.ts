// Fixed key-agreement fixtures generated with Node.
const vectors = [
  {
    "curve": "P-256",
    "jwk": {"key_ops":["deriveBits","deriveKey"],"ext":true,"kty":"EC","x":"PsxhjpFHFqPOwXec08QpUmwc6oth1chdH8IDtuLHtVM","y":"cBJ78OlBLb1W5NEns2JtIf8AG9H0TrBjzwggPGKXCyQ","crv":"P-256","d":"OWyITcivpbE33Y-h46lf2fq_9bE63ynLq4EjPzsFkXg"},
    "peer": {"key_ops":[],"ext":true,"kty":"EC","x":"p9onqz_477rki3DL8t5hU8CqBBTgrLinVnW2JstshGg","y":"swwR7R0GCHCLZT6csEt2b89NEn5fiIpNgyCGIvDaTR8","crv":"P-256"},
    "secret": [121,90,69,2,85,153,223,225,203,157,233,166,179,182,236,91,40,0,133,241,253,147,62,24,6,143,191,60,150,100,188,73]
  },
  {
    "curve": "P-384",
    "jwk": {"key_ops":["deriveBits","deriveKey"],"ext":true,"kty":"EC","x":"4ZG-eiWsXyrzEKTAZDB08G3RqqH-2QOYcjj9pjvf4gpWBpzcuidRlpCmzoSfbN7O","y":"IwYUNdJ_pn1E6_mh8FY8quKccYVU1AabvznXNoEUmBPF_KJTg-YqcGFWqoJMFmbq","crv":"P-384","d":"UxyXWWPK1apWkSkJgWnys1h-jKYv8aMViil_8viDX4-SxpbCzg2mGRtlXuECAKtA"},
    "peer": {"key_ops":[],"ext":true,"kty":"EC","x":"HhKOvEAPW5QC8RsCpt7yX3dYVTwv4ioOE1XJa-N79KHUKuRczwdhOxROmwnCPWVU","y":"aki_Kp1NU1DD7ix-KWlvSAyUSWyIbsNYX4YaeE1KDanOxgT7GJSbiOs5Os_9tTMq","crv":"P-384"},
    "secret": [116,141,245,117,55,241,36,213,198,28,40,154,44,237,162,12,178,95,107,180,15,254,163,113,129,11,159,95,146,160,104,88,64,16,19,171,150,171,115,247,229,68,173,24,146,29,202,228]
  },
  {
    "curve": "P-521",
    "jwk": {"key_ops":["deriveBits","deriveKey"],"ext":true,"kty":"EC","x":"AJHY_wUSInZ9GMuKtttgyFPVP6wBfHbVb-OkOiqsUPLxH9ibsP4wZLCsbEkq5NlifKU6quwB4963OWIV2Uo9WoqX","y":"AeKkbSD1Hg4a5r2RNL-FgqZu9fdA9dT2jZMuP1AzfgIGPR6eE43iAIkN08f9QwnP9wlM2DTKAacz1Hnn-1v5LqP-","crv":"P-521","d":"AZoT3XS3tnCEbQu4KuF5IYnvxBg9gIziCQDqsJJ-boBD93D_RVqb4qbPb80zmp49sZP42hNY1eYfs2u2l2vGEG2c"},
    "peer": {"key_ops":[],"ext":true,"kty":"EC","x":"AAu8SwwqmQRN1-c4xzLaYe9uGzTvTzvyKoeOJYsS67DPSX8hfloD_31d-OVDFlB1LqDHzo6JHe6s6UaRVLc2drmu","y":"AHFyE6_GzDWVQtouFptoSYmazraOUy9sf18zxJpq46OfKngIKz8KzGhNEM8IwxqR7NibR539vK881S4QLlFqi3OC","crv":"P-521"},
    "secret": [0,216,251,101,63,213,225,208,94,233,113,219,137,46,105,122,111,82,36,199,24,7,201,138,58,95,58,85,250,214,23,99,137,250,139,101,115,57,236,36,161,122,248,65,158,145,176,81,139,171,174,102,22,199,239,209,58,198,164,249,226,203,234,75,251,126]
  },
];
async function run():Promise<void>{
 for(const vector of vectors){
  const algorithm={name:'ECDH',namedCurve:vector.curve};
  const privateKey=await crypto.subtle.importKey('jwk',vector.jwk,algorithm,true,['deriveBits','deriveKey']);
  const publicKey=await crypto.subtle.importKey('jwk',vector.peer,algorithm,true,[]);
  const derive={name:'ECDH',public:publicKey};
  console.log('metadata',privateKey.type,publicKey.type,privateKey.algorithm.name,(privateKey.algorithm as EcKeyAlgorithm).namedCurve,publicKey.usages.length);
  const full=new Uint8Array(await crypto.subtle.deriveBits(derive,privateKey,null));
  console.log('node vector',vector.curve,full.length,Array.from(full).join(',')===vector.secret.join(','));
  for(const bits of [0,1,7,8,9,127,128,255,256,full.length*8]){
   const actual=new Uint8Array(await crypto.subtle.deriveBits(derive,privateKey,bits));
   const expected=vector.secret.slice(0,Math.ceil(bits/8));
   if(bits%8!==0)expected[expected.length-1]&=255<<(8-bits%8);
   console.log('bits',bits,actual.length,Array.from(actual).join(',')===expected.join(','));
  }
  for(const target of [{name:'AES-GCM',length:128},{name:'HMAC',hash:'SHA-256',length:128}]){
   const key=await crypto.subtle.deriveKey(derive,privateKey,target,true,target.name==='HMAC'?['sign']:['encrypt']);
   console.log('derived key',target.name,Array.from(new Uint8Array(await crypto.subtle.exportKey('raw',key))).join(',')===vector.secret.slice(0,16).join(','));
  }
  const cloned=structuredClone(privateKey);
  console.log('clone',Array.from(new Uint8Array(await crypto.subtle.deriveBits(derive,cloned,null))).join(',')===vector.secret.join(','));
  for(const format of ['jwk','pkcs8'] as const){
   const exported=await crypto.subtle.exportKey(format as any,privateKey);
   const restored=await crypto.subtle.importKey(format as any,exported as any,algorithm,true,['deriveBits']);
   console.log('private roundtrip',format,Array.from(new Uint8Array(await crypto.subtle.deriveBits(derive,restored,null))).join(',')===vector.secret.join(','));
  }
  for(const format of ['jwk','raw','spki'] as const){
   const exported=await crypto.subtle.exportKey(format as any,publicKey);
   const restored=await crypto.subtle.importKey(format as any,exported as any,algorithm,true,[]);
   console.log('public roundtrip',format,Array.from(new Uint8Array(await crypto.subtle.deriveBits({name:'ECDH',public:restored},privateKey,null))).join(',')===vector.secret.join(','));
  }
 }
 const first=await crypto.subtle.generateKey({name:'ECDH',namedCurve:'P-256'},false,['deriveBits']);
 const second=await crypto.subtle.generateKey({name:'ECDH',namedCurve:'P-256'},false,['deriveBits']);
 const a=await crypto.subtle.deriveBits({name:'ECDH',public:second.publicKey},first.privateKey,null);
 const b=await crypto.subtle.deriveBits({name:'ECDH',public:first.publicKey},second.privateKey,null);
 console.log('generated',Array.from(new Uint8Array(a)).join(',')===Array.from(new Uint8Array(b)).join(','),first.privateKey.extractable,first.publicKey.extractable);
}
run();

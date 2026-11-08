// Fixed RSA-PSS interoperability fixture generated with Node.
const vector = {"jwk":{"key_ops":["sign"],"ext":true,"alg":"PS256","kty":"RSA","n":"zdRd0_-g_TBfpsUNDP5lAAZ5zawgpXkj8Tfx3rBOM6jFTIUcojACIY55zARZ_k_YrZ7QlhU8rM3nJ_UtEetnVKQM_Xuj-IrNsGpWyMPMeFQq0QNNZ3WdLt6m7wvRu8JH-ek1-sRV3dfGUVxR1Bnf9aiCiL4lad57p6ztUc103gM","e":"AQAB","d":"VPrjKk4Wd7JrK32b38uAg7wouePhLrCPSF5ivCBqiSWbH_atVcEggB5E5xi81yS8PyKc1_o4rNfuIi81xno8lJWbfwiiFyzF7Bxj7aCcphMHZUD0bunjZu00XQAToOGeAab9XT-bypDokD6MAJsfHmsXNVQL62ialAfXgy5CXJE","p":"9P-QsiRo-N4-dYNbeUd6fT4MGAixlFLHo7L6bvmFjKb8CqwvPIqzKok8E-BoK_GBA4e7Fo6M_lA5-RHtzkaTvw","q":"1xKHJQinoY4GUsGB35sWGduuADMOpD6H_3yhJGXpcuQUBJTX6lTJfC5oTbhU3xR-bODp5M9jj0u5EPCv_bC2vQ","dp":"YYGxMeUhziKNkzFzX4JeMqhWId6b5l9Hy1P5Ld-rEj6HQywiJKNDCWPTAEooy4xzf1cu05xqavC9Ee7byWP3ew","dq":"Ep-lGjzcny24OHRPNbitOkH-mD_Tlb9gcWwuXAODKpK7dQYaiE0dQWaXW5uJpeOVawIy3sBio6bv-JqO8s3ZlQ","qi":"Vf7YhgusJsCIxiSGQwlKHC0Huk78PX5dmiSNQeG4h2HSepq3Ytef_8LS7R6pb2yWDfZ9DqmhaVEwHHw6eZD9Xw"},"spki":[48,129,159,48,13,6,9,42,134,72,134,247,13,1,1,1,5,0,3,129,141,0,48,129,137,2,129,129,0,205,212,93,211,255,160,253,48,95,166,197,13,12,254,101,0,6,121,205,172,32,165,121,35,241,55,241,222,176,78,51,168,197,76,133,28,162,48,2,33,142,121,204,4,89,254,79,216,173,158,208,150,21,60,172,205,231,39,245,45,17,235,103,84,164,12,253,123,163,248,138,205,176,106,86,200,195,204,120,84,42,209,3,77,103,117,157,46,222,166,239,11,209,187,194,71,249,233,53,250,196,85,221,215,198,81,92,81,212,25,223,245,168,130,136,190,37,105,222,123,167,172,237,81,205,116,222,3,2,3,1,0,1],"bytes":[44,78,74,247,190,98,31,236,109,212,226,243,131,93,235,110,111,134,170,66,167,184,128,35,235,61,42,114,53,14,221,59,26,159,30,171,82,241,9,252,237,2,98,43,45,211,77,163,1,184,165,225,210,79,254,91,139,198,152,254,186,93,58,150,37,70,211,151,133,139,175,249,133,159,119,126,207,194,202,11,116,67,163,35,17,90,93,168,248,28,170,141,254,65,50,58,241,6,14,102,80,89,33,138,60,187,40,229,14,176,196,137,169,31,47,183,75,171,171,143,39,26,1,121,232,213,195,140]};
async function run():Promise<void>{
 const keyAlgorithm={name:'RSA-PSS',hash:'SHA-256'};
 const data=new TextEncoder().encode('PSS vector');
 const privateKey=await crypto.subtle.importKey('jwk',vector.jwk,keyAlgorithm,true,['sign']);
 const publicKey=await crypto.subtle.importKey('spki',new Uint8Array(vector.spki),keyAlgorithm,true,['verify']);
 console.log('node vector',await crypto.subtle.verify({name:'RSA-PSS',saltLength:32},publicKey,new Uint8Array(vector.bytes),data));
 const signature=await crypto.subtle.sign({name:'RSA-PSS',saltLength:32},privateKey,data);
 console.log('sign',await crypto.subtle.verify({name:'RSA-PSS',saltLength:32},publicKey,signature,data));
 async function failure(label:string,operation:()=>Promise<unknown>):Promise<void>{try{await operation();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
 await failure('missing salt',()=>crypto.subtle.sign({name:'RSA-PSS'} as RsaPssParams,privateKey,data));
 await failure('public sign',()=>crypto.subtle.sign({name:'RSA-PSS',saltLength:32},publicKey,data));
 await failure('private verify',()=>crypto.subtle.verify({name:'RSA-PSS',saltLength:32},privateKey,signature,data));
 await failure('JWK algorithm',()=>crypto.subtle.importKey('jwk',{...vector.jwk,alg:'RS256'},keyAlgorithm,true,['sign']));
 const pkcs=await crypto.subtle.importKey('spki',new Uint8Array(vector.spki),{name:'RSASSA-PKCS1-v1_5',hash:'SHA-256'},true,['verify']);
 await failure('key algorithm',()=>crypto.subtle.verify({name:'RSA-PSS',saltLength:32},pkcs,signature,data));
 let names=0,salts=0;
 await crypto.subtle.sign({get name(){names++;return 'RSA-PSS';},get saltLength(){salts++;return 32;}},privateKey,data);
 console.log('getters',names,salts);
}
run();

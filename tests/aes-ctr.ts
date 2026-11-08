export {};
function hex(value: ArrayBuffer): string {
    return Array.from(new Uint8Array(value), byte => byte.toString(16).padStart(2, "0")).join("");
}
async function main(): Promise<void> {
    const material = Uint8Array.from([43,126,21,22,40,174,210,166,171,247,21,136,9,207,79,60]);
    const key = await crypto.subtle.importKey("raw", material, "AES-CTR", true, ["encrypt", "decrypt", "wrapKey", "unwrapKey"]);
    const counter = Uint8Array.from([240,241,242,243,244,245,246,247,248,249,250,251,252,253,254,255]);
    const plain = Uint8Array.from([107,193,190,226,46,64,159,150,233,61,126,17,115,147,23,42]);
    const vector = hex(await crypto.subtle.encrypt({name:"AES-CTR", counter, length:128}, key, plain));
    if (vector !== "874d6191b620e3261bef6864990db6ce") throw new Error("NIST vector failed");
    console.log("vector", vector);
    for (const bits of [1, 2, 7, 8, 9, 63, 64, 65, 127, 128]) {
        const initial = new Uint8Array(16); initial.fill(255);
        for (const size of [0, 1, 15, 16, 17, 31, 32]) {
            const input = Uint8Array.from({length:size}, (_, i) => (i * 13) & 255);
            const params = {name:"aes-ctr", counter:initial, length:bits};
            const encrypted = await crypto.subtle.encrypt(params, key, input);
            const restored = await crypto.subtle.decrypt(params, key, encrypted);
            console.log("roundtrip", bits, size, hex(encrypted), hex(restored) === hex(input.buffer), initial[15]);
        }
    }
    const backing = new Uint8Array(40); backing.fill(255);
    const view = new DataView(backing.buffer, 8, 16);
    console.log("views",hex(await crypto.subtle.encrypt({name:"AES-CTR",counter:view,length:9},key,new DataView(backing.buffer,3,17))));
    for (let bits=1;bits<=128;bits++) {
        const params={name:"AES-CTR",counter:new Uint8Array(16).fill(255),length:bits};
        const result=await crypto.subtle.encrypt(params,key,new Uint8Array(32));
        console.log("all widths",bits,hex(result));
    }
    const large = Uint8Array.from({length:8209}, (_, i) => i & 255);
    const params = {name:"AES-CTR", counter, length:64};
    const encrypted = await crypto.subtle.encrypt(params, key, large);
    console.log("batches", encrypted.byteLength, hex(encrypted.slice(4080,4120)), hex(await crypto.subtle.decrypt(params,key,encrypted)) === hex(large.buffer));
    const jwk = await crypto.subtle.exportKey("jwk",key);
    console.log("jwk",jwk.alg,jwk.kty,jwk.key_ops!.join(","),(key.algorithm as AesKeyAlgorithm).length);
    const imported = await crypto.subtle.importKey("jwk",jwk,"AES-CTR",false,["encrypt"]);
    console.log("jwk import",hex(await crypto.subtle.encrypt(params,imported,plain)));
    for (const size of [128,192,256]) {
        const generated = await crypto.subtle.generateKey({name:"AES-CTR",length:size},true,["encrypt","decrypt"]);
        console.log("generated",generated.algorithm.name,(generated.algorithm as AesKeyAlgorithm).length,(await crypto.subtle.exportKey("raw",generated)).byteLength);
    }
    for (const format of ["raw","jwk"] as const) {
        const wrapped = await crypto.subtle.wrapKey(format,key,key,params);
        const restored = await crypto.subtle.unwrapKey(format,wrapped,key,params,"AES-CTR",true,["encrypt"]);
        console.log("wrap",format,hex(await crypto.subtle.exportKey("raw",restored)) === hex(material.buffer));
    }
    const wrapOnly=await crypto.subtle.importKey("raw",material,"AES-CTR",false,["wrapKey"]);
    const unwrapOnly=await crypto.subtle.importKey("raw",material,"AES-CTR",false,["unwrapKey"]);
    const wrappedOnly=await crypto.subtle.wrapKey("raw",key,wrapOnly,params);
    console.log("restricted wrap",hex(await crypto.subtle.exportKey("raw",await crypto.subtle.unwrapKey("raw",wrappedOnly,unwrapOnly,params,"AES-CTR",true,["encrypt"]))) === hex(material.buffer));
    const base = await crypto.subtle.importKey("raw",material,"HKDF",false,["deriveKey"]);
    const derived = await crypto.subtle.deriveKey({name:"HKDF",hash:"SHA-256",salt:new Uint8Array(),info:new Uint8Array()},base,{name:"AES-CTR",length:128},true,["encrypt"]);
    console.log("derived",derived.algorithm.name,hex(await crypto.subtle.exportKey("raw",derived)));
    const failures: [string, () => Promise<unknown>][] = [];
    for (const bits of [0,129,255,256,-1,NaN,Infinity]) failures.push(["bits " + bits,()=>crypto.subtle.encrypt({name:"AES-CTR",counter,length:bits},key,plain)]);
    for (const bytes of [0,15,17]) failures.push(["counter " + bytes,()=>crypto.subtle.encrypt({name:"AES-CTR",counter:new Uint8Array(bytes),length:64},key,plain)]);
    failures.push(["counter reuse",()=>crypto.subtle.encrypt({name:"AES-CTR",counter,length:1},key,new Uint8Array(33))]);
    failures.push(["decrypt reuse",()=>crypto.subtle.decrypt({name:"AES-CTR",counter,length:1},key,new Uint8Array(33))]);
    const restricted = await crypto.subtle.importKey("raw",material,"AES-CTR",false,["encrypt"]);
    failures.push(["permission",()=>crypto.subtle.decrypt(params,restricted,plain)]);
    const wrong = await crypto.subtle.importKey("raw",material,"AES-GCM",false,["encrypt"]);
    failures.push(["algorithm",()=>crypto.subtle.encrypt(params,wrong,plain)]);
    for (const [label,run] of failures) try { await run(); console.log(label,"accepted"); } catch (error) {console.log(label,(error as Error).name);}
}
main().catch(error => { console.log("unexpected",String(error)); throw error; });

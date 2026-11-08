async function main(): Promise<void> {
    const data = new TextEncoder().encode('ECDSA test');
    for(const namedCurve of ['P-256','P-384','P-521']) {
        const pair = await crypto.subtle.generateKey({name:'ECDSA',namedCurve},true,['sign','verify']);
        console.log('keys',namedCurve,pair.privateKey.type,pair.publicKey.type,pair.privateKey.usages.join(','),pair.publicKey.usages.join(','),(pair.publicKey.algorithm as EcKeyAlgorithm).namedCurve);
        for(const hash of ['SHA-1','SHA-256','SHA-384','SHA-512']) {
            const signature = await crypto.subtle.sign({name:'ECDSA',hash},pair.privateKey,data);
            console.log('signature',hash,signature.byteLength,await crypto.subtle.verify({name:'ECDSA',hash},pair.publicKey,signature,data));
            const changed = new Uint8Array(signature);changed[0]^=1;
            console.log('tampered',await crypto.subtle.verify({name:'ECDSA',hash},pair.publicKey,changed,data),await crypto.subtle.verify({name:'ECDSA',hash},pair.publicKey,new Uint8Array(3),data));
        }
        const clone = structuredClone(pair);
        const cloneSignature = await crypto.subtle.sign({name:'ECDSA',hash:'SHA-256'},clone.privateKey,data);
        console.log('clone',await crypto.subtle.verify({name:'ECDSA',hash:'SHA-256'},clone.publicKey,cloneSignature,data),clone.privateKey.type);
        for(const format of ['raw','spki','jwk'] as const) {
            const exported = await crypto.subtle.exportKey(format as any,pair.publicKey);
            const restored = await crypto.subtle.importKey(format as any,exported as any,{name:'ECDSA',namedCurve},true,['verify']);
            const signature = await crypto.subtle.sign({name:'ECDSA',hash:'SHA-256'},pair.privateKey,data);
            console.log('public roundtrip',format,await crypto.subtle.verify({name:'ECDSA',hash:'SHA-256'},restored,signature,data));
        }
        for(const format of ['pkcs8','jwk'] as const) {
            const exported = await crypto.subtle.exportKey(format as any,pair.privateKey);
            const restored = await crypto.subtle.importKey(format as any,exported as any,{name:'ECDSA',namedCurve},true,['sign']);
            const signature = await crypto.subtle.sign({name:'ECDSA',hash:'SHA-256'},restored,data);
            console.log('private roundtrip',format,await crypto.subtle.verify({name:'ECDSA',hash:'SHA-256'},pair.publicKey,signature,data));
        }
    }
}
main();

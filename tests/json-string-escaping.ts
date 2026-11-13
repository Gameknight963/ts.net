export {};
for (const length of [0,1,15,16,31,32,33,63,64,65,127,128]) {
    const plain='x'.repeat(length);
    console.log('plain',length,JSON.stringify(plain));
    for (const code of [0,1,8,9,10,12,13,31,34,92,127,128,255,0x2028,0xd800,0xdc00]) {
        for (const position of [0,Math.floor(length/2),length]) {
            const text=plain.slice(0,position)+String.fromCharCode(code)+plain.slice(position);
            console.log(length,code,position,JSON.stringify(text));
        }
    }
    console.log('pair',length,JSON.stringify(plain+String.fromCharCode(0xd83d,0xde00)+plain));
}
const long='aZ0_- '.repeat(20000);console.log('long',JSON.stringify(long)==='"'+long+'"');
console.log('key',JSON.stringify({[long.slice(0,100)]:long.slice(0,100)}));

const wide=String.fromCharCode(233).repeat(1000);console.log('wide',wide.charCodeAt(0),JSON.stringify(wide)==='"'+wide+'"');
const paired=String.fromCharCode(0xd83d,0xde00).repeat(1000);console.log('paired',JSON.stringify(paired)==='"'+paired+'"');

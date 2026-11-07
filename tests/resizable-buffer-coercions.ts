const source=new ArrayBuffer(8,{maxByteLength:16});new Uint8Array(source).set([1,2,3,4,5,6,7,8]);
console.log("slice",Array.from(new Uint8Array(source.slice({valueOf(){source.resize(3);return 1;}} as any))).join("|"));
const value=new ArrayBuffer(8,{maxByteLength:16});new Uint8Array(value).set([1,2,3,4,5,6,7,8]);
const typed=new Uint8Array(value);
console.log("typed-slice",Array.from(typed.slice({valueOf(){value.resize(3);return 1;}} as any)).join("|"));

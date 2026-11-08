export {};
const view=new DataView(new ArrayBuffer(8));
function check(value:bigint):void {
 view.setFloat64(0,Number(value));console.log(view.getBigUint64(0).toString(16));
 view.setFloat64(0,Number(-value));console.log(view.getBigUint64(0).toString(16));
}
for(const bits of [0,1,32,52,53,54,55,63,64,65,100,128,500,1023,1024,2048]) {
 const base=1n<<BigInt(bits),step=bits>53?1n<<BigInt(bits-53):1n;
 for(const delta of [-3n,-2n,-1n,0n,1n,2n,3n,4n,5n]) {
  check(base+delta*step);check(base+delta*step+1n);check(base+delta*step-1n);
 }
}
const threshold=(1n<<1024n)-(1n<<970n);
check(threshold-1n);check(threshold);check(threshold+1n);
let state=123456789n;
for(let index=0;index<512;index++) {
 state=(state*6364136223846793005n+1442695040888963407n)&((1n<<128n)-1n);
 check(state<<BigInt(index%900));
}
console.log('example',Number(9007199254740995n),Number(-9007199254740995n));

export {};
function kernel(value:any):void {value.sort();}
function make(kind:number):any {
 if(kind===4)return new Int32Array(10000);
 if(kind===5)return new BigInt64Array(10000);
 if(kind===0)return new Int32Array([4,1,3,2]);
 if(kind===1)return new BigInt64Array([4n,1n,3n,2n]);
 if(kind===2){const value=new Int32Array(new SharedArrayBuffer(16));value.set([4,1,3,2]);return value;}
 return new Int32Array([99,4,1,3,2,88]).subarray(1,5);
}
function checksum(value:any):number {let total=0;for(let i=0;i<value.length;i++)total+=Number(value[i])*(i+1);return total;}
for(let kind=0;kind<6;kind++){const value=make(kind);kernel(value);console.log(kind,checksum(value));}

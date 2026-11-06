function membershipKernel(count:number,offset:number):number {
 const values:number[]=[1,,undefined as any,4];
 const properties={present:undefined};
 let total=offset;
 for(let i=0;i<count;i++) {
  if((i&3) in values)total++;
  if('present' in properties)total++;
 }
 return total;
}
console.log(membershipKernel(1,0),membershipKernel(100000,0));

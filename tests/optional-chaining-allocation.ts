function optionalKernel(count:number,offset:number):number {
 const callback=(value:number):number=>value+1;
 const point={value:3};
 let sum=0;
 for(let i=0;i<count;i++)sum+=(callback?.(i)??0)+(point?.value??0)+offset;
 return sum;
}
console.log(optionalKernel(1,0),optionalKernel(100000,0));

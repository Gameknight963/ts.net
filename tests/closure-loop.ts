function kernel(count: number, seed: number): number {
 let value=seed;
 const increment=(delta: number): number => value+=delta;
 let total=0;
 for(let i=0;i<count;i++) total=increment(1);
 return total;
}
console.log(kernel(8, 3));

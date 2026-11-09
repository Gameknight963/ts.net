export {};
for(const radix of [6,12,14,18,20,22,24,26,28,30,34,36])for(const value of [0.5,-0.5,0.25,0.75,1.5,10.125,100.5])console.log(radix,value,value.toString(radix));
for(const radix of [3,5,7,9,11,13,15,17,19,21,23,25,27,29,31,33,35])for(const digit of [1,2])console.log('reciprocal',radix,digit,(digit/radix).toString(radix));
// Non-decimal final digits are not identical across engines. Check the value
// and alphabet for the broader matrix, rather than requiring V8's rounding.
let cases=0;
for(let radix=2;radix<=36;radix++)for(const value of [0.1,0.2,0.3,Math.PI,1/3,1e20,1e100,1e-100]) {
 if(radix===10)continue;
 const text=value.toString(radix);let parsed=0,scale=1,fraction=false;
 for(const char of text) {
  if(char==='.') {if(fraction)throw new Error('Repeated radix point');fraction=true;continue;}
  const digit='0123456789abcdefghijklmnopqrstuvwxyz'.indexOf(char);
  if(digit<0 || digit>=radix)throw new Error('Invalid radix digit');
  if(fraction){scale/=radix;parsed+=digit*scale;}else parsed=parsed*radix+digit;
 }
 if(Math.abs(parsed-value)>Math.abs(value)*1e-13)throw new Error('Radix conversion lost numeric value');
 cases++;
}
console.log('approximate conversions',cases);
const method=Number.prototype.toString,trace:string[]=[];
const radix={valueOf(){trace.push('radix');return 6;}};
console.log(method.call(new Number(1.5),radix as any),trace.join(','));

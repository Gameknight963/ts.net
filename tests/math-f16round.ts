const f16round:any=(Math as any).f16round;
for(const value of [undefined,null,true,false,'1.0005','bad',NaN,Infinity,-Infinity,0,-0,1,-1,65504,65519,65520,-65520,2**-24,2**-25,-(2**-25),2**-14]) {
    const result=f16round(value);console.log('round',result,1/result);
}
let calls=0;console.log('coercion',f16round({valueOf():number {calls++;return 1.0005;}}),calls);
try{f16round({valueOf(){throw new Error('conversion failed');}});}catch(error){console.log('error',(error as Error).message);}
console.log('metadata',f16round.name,f16round.length,Object.getOwnPropertyDescriptor(Math,'f16round')!.enumerable);
console.log('receiver ignored',f16round.call(null,1.0005));
function half(bits:number):number {
    const exponent=bits>>>10,significand=bits&1023;
    return exponent===0?significand*2**-24:(1024+significand)*2**(exponent-25);
}
let exact=0,ties=0,neighbors=0;
for(let bits=0;bits<31743;bits++) {
    const lower=half(bits),upper=half(bits+1),midpoint=(lower+upper)/2;
    if(!Object.is(f16round(lower),lower)||!Object.is(f16round(-lower),-lower))throw new Error('exact '+bits);
    const expected=bits%2===0?lower:upper;
    if(!Object.is(f16round(midpoint),expected)||!Object.is(f16round(-midpoint),-expected))throw new Error('tie '+bits);
    const step=2**(Math.floor(Math.log2(midpoint))-52);
    if(!Object.is(f16round(midpoint-step),lower)||!Object.is(f16round(midpoint+step),upper))throw new Error('neighbor '+bits);
    if(!Object.is(f16round(-midpoint+step),-lower)||!Object.is(f16round(-midpoint-step),-upper))throw new Error('negative neighbor '+bits);
    exact+=2;ties+=2;neighbors+=4;
}
console.log('boundaries',exact,ties,neighbors);
const overflow=65520,step=2**-37;
console.log('overflow',f16round(overflow-step),f16round(overflow),f16round(overflow+step),f16round(-overflow+step),f16round(-overflow));

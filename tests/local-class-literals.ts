export {};
class Holder {value = -1; other = +2; zero = -0;}
function kernel(count: number): number {let total=0;for(let i=0;i<count;i++){const value=new Holder();total += value.value+value.other;}return total;}
function escape(): Holder {return new Holder();}
console.log('kernel',kernel(10),escape().value,escape() instanceof Holder);
function zero(): number {return new Holder().zero;}
console.log('signed zero',Object.is(zero(),-0));
class Default {constructor(public value = -3) {}}
function defaultValue(): number {return new Default().value;}
console.log('default',defaultValue());
let effects=0;
function number(): number {effects++;return 5;}
class Effect {value = -number();}
console.log('effect',new Effect().value,effects);
class Coercion {value = -({valueOf(){effects++;return 7;}} as any);}
console.log('coercion',new Coercion().value,effects);

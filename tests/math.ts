console.log('constants', Math.PI, Math.E, Math.LN2, Math.LN10, Math.LOG2E, Math.LOG10E, Math.SQRT1_2, Math.SQRT2);
for(const value of [-Infinity, -3.5, -1.5, -0.6, -0.5, -0.1, -0, 0, 0.1, 0.5, 1.5, 2.5, 4503599627370495.5, Infinity, NaN]) {
 console.log('rounding', value, Math.abs(value), Math.floor(value), Math.ceil(value), Math.trunc(value), Math.round(value), Math.sign(value));
 console.log('zeros', 1/Math.ceil(value), 1/Math.trunc(value), 1/Math.round(value), 1/Math.sign(value));
}
console.log('limits', Math.min(), Math.max(), Math.min(3,-1,2), Math.max(3,-1,2), Math.min(NaN,1), Math.max(1,NaN));
console.log('zero-extremes', 1/Math.min(0,-0), 1/Math.min(-0,0), 1/Math.max(0,-0), 1/Math.max(-0,0));
console.log('integer', Math.imul(0xffffffff,5), Math.imul(0x80000000,2), Math.imul(3.9,4.9), Math.clz32(0), Math.clz32(1), Math.clz32(-1), Math.clz32(NaN));
console.log('fround', Math.fround(1.1), Math.fround(1e100), 1/Math.fround(-0), Math.fround(NaN));
console.log('power', Math.pow(2,10), Math.pow(NaN,0), Math.pow(-1,Infinity), Math.pow(-0,-3), Math.pow(-2,0.5));
console.log('roots', Math.sqrt(9), Math.sqrt(-1), 1/Math.sqrt(-0), Math.cbrt(-8));
console.log('logs', Math.log(1), Math.log(0), Math.log(-1), Math.log2(8), Math.log10(1000));
console.log('special', Math.exp(-Infinity), Math.expm1(-Infinity), Math.log1p(-1), Math.log1p(-2), Math.log1p(Infinity), 1/Math.expm1(-0), 1/Math.log1p(-0));
console.log('tiny', Math.expm1(1e-20), Math.log1p(1e-20));
console.log('trig', Math.sin(0), Math.cos(0), Math.tan(0), Math.asin(0), Math.acos(1), Math.atan(0), Math.atan2(0,1));
console.log('hyperbolic', Math.sinh(0), Math.cosh(0), Math.tanh(0), Math.asinh(0), Math.acosh(1), Math.atanh(0));
console.log('domains', Math.acos(2), Math.asin(2), Math.acosh(0), Math.atanh(2), Math.sin(Infinity));
console.log('hypot', Math.hypot(), Math.hypot(3,4), Math.hypot(-3,4,12), Math.hypot(NaN,Infinity), Math.hypot(NaN,1));
console.log('scaled', Math.hypot(1e300,1e300)/1e300 > 1.4142, Math.hypot(1e-300,1e-300)/1e-300 > 1.4142);
const dynamic: any = Math;
console.log('coercion', dynamic.abs('-2'), dynamic.pow('2','3'), dynamic.min(null,undefined), dynamic.clz32(undefined));
let effects = '';
console.log('convert-all', dynamic.max(NaN, {valueOf():number {effects+='A';return 2;}}, {valueOf():number {effects+='B';return 3;}}), effects);
effects = '';
console.log('hypot-convert-all', dynamic.hypot(Infinity, {valueOf():number {effects+='A';return NaN;}}), effects);
try {dynamic.min(NaN, {valueOf():number {throw 'conversion';}});} catch(error) {console.log('conversion-throw', error);}
console.log('metadata', Math.abs.name, Math.abs.length, Math.pow.length, Math.min.length, Math.hypot.length, Math.random.length, Object.keys(Math).length);
console.log('borrowed', Math.abs.call(null,-2), Math.pow.apply(undefined,[2,3]));
try {dynamic.PI=1;} catch(error) {console.log('readonly', error instanceof TypeError);}
const original = Math.abs; Math.abs = () => 7; console.log('override',Math.abs(-2)); Math.abs = original;
let randomValid = true; for(let i=0;i<100;i++) {const value=Math.random();if(!(value>=0 && value<1)) randomValid=false;}
console.log('random', randomValid);
function near(actual:number, expected:number):boolean {return Math.abs(actual - expected) <= 1e-14 * Math.max(1,Math.abs(expected));}
console.log('accuracy', near(Math.sin(Math.PI/6),0.5), near(Math.cos(Math.PI/3),0.5), near(Math.tan(Math.PI/4),1), near(Math.asin(0.5),Math.PI/6), near(Math.acos(0.5),Math.PI/3), near(Math.atan(1),Math.PI/4));
console.log('inverse-hyperbolic', near(Math.asinh(Math.sinh(0.5)),0.5), near(Math.acosh(Math.cosh(0.5)),0.5), near(Math.atanh(Math.tanh(0.5)),0.5));
console.log('small-accuracy', near(Math.expm1(0.5),0.6487212707001282), near(Math.expm1(-0.5),-0.3934693402873666), near(Math.log1p(0.5),0.4054651081081644));
console.log('near-half', Math.round(0.49999999999999994), Math.round(4503599627370495), Math.round(-1.5000000000000002));
for(const fn of [Math.sin,Math.tan,Math.asin,Math.atan,Math.sinh,Math.tanh,Math.asinh,Math.atanh,Math.cbrt]) console.log('unary-zero', fn.name, 1/fn(-0), fn(NaN));
console.log('missing', dynamic.abs(), dynamic.pow(2), dynamic.imul(), dynamic.clz32(), dynamic.fround());
console.log('two-conversions', dynamic.pow({valueOf():number {effects='L';return 2;}}, {valueOf():number {effects+='R';return 3;}}), effects);
console.log('destructured', (({abs,pow}:typeof Math) => abs(-2)+pow(2,3))(Math));
function shadow():number {const Math={abs(value:number):number {return value + 10;}};return Math.abs(-2);}
console.log('shadow', shadow());
let mathKeys='';for(const key in Math) mathKeys+=key;console.log('enumeration', mathKeys);
try {new (Math.abs as any)(1);} catch(error) {console.log('not-constructor', error instanceof TypeError);}

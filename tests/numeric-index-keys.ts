export {};
const object={field0:0,field1:1,field2:2,field3:3,field4:4,field5:5,field6:6,field7:7,field8:8,field9:9,field10:10,field11:11,field12:12,field13:13,field14:14,field15:15,field16:16,field17:17,field18:18,field19:19,field20:20,field21:21,field22:22,field23:23,field24:24,field25:25,field26:26,field27:27,field28:28,field29:29,field30:30,field31:31,field32:32,field33:33,field34:34,field35:35,field36:36,field37:37,field38:38,field39:39};
function fixed(target: typeof object, key: keyof typeof object): number { return target[key]; }
for (const key of Object.keys(object) as (keyof typeof object)[]) console.log("fixed", key, fixed(object,key));
console.log("missing", fixed(object,"missing" as any));
function indexed(target: {[key:string]:number}, key:string):number { return target[key]; }
const array:any=[3,4];
console.log("array", indexed(array,"0"),indexed(array,"length"),indexed(array,"missing"));
let reads=0;Object.defineProperty(array,"1",{get(){reads++;return 7;},configurable:true});
console.log("array getter",indexed(array,"1"),reads);
const typed:any=new Float64Array([3,4]);
console.log("typed",indexed(typed,"0"),indexed(typed,"length"),indexed(typed,"missing"));
console.log("string",indexed("abc" as any,"length"));
function indexedAny(target: {[key:string]:number}, key:any):number { return target[key]; }
let conversions=0;
const key:any={toString(){conversions++;return "field39";}};
console.log("coercion",indexedAny(object,key),conversions);
try{indexedAny(null as any,key);}catch{console.log("null",conversions);}
try{indexedAny(undefined as any,key);}catch{console.log("undefined",conversions);}
const symbol=Symbol("key");const symbols:any={[symbol]:7};
console.log("symbol",indexedAny(symbols,symbol));
const proto={get value(){return (this as any).amount;}};
const inherited:any=Object.create(proto);inherited.amount=19;
console.log("inherited getter",indexed(inherited,"value"));
const changed:any={value:3};let calls=0;
const changingKey:any={toString(){calls++;Object.defineProperty(changed,"value",{get(){return 23;},configurable:true});return "value";}};
console.log("coercion mutation",indexedAny(changed,changingKey),calls);

const unusual:any={undefined:17,null:18,NaN:19,Infinity:20,"-0":21,"0":22};
for(const key of [undefined,null,NaN,Infinity,-0,"-0"])console.log("unusual key",String(key),indexedAny(unusual,key));

function checked(target:any,key:string):number{return target[key];}
console.log("checked object",checked(object,"field0"),checked(object,"field39"),checked(object,"missing"));
console.log("checked array",checked(array,"0"),checked(array,"length"),checked(array,"missing"));
console.log("checked typed array",checked(typed,"1"),checked(typed,"length"));
console.log("checked string",checked("abc","length"));
console.log("checked prototype",checked(inherited,"value"));
let checkedReads=0;
const accessor={get value(){checkedReads++;return 29;}};
console.log("checked getter",checked(accessor,"value"),checkedReads);
const order:string[]=[];
function nullTarget():any{order.push("receiver");return null;}
function propertyName():string{order.push("key");return "field0";}
try{checked(nullTarget(),propertyName());}catch{console.log("checked order",order.join(","));}

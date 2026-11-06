const source={x:1,y:2,z:3};const {x,...rest}=source;console.log(x,rest.y,rest.z,Object.keys(rest).length);rest.y=4;console.log(source.y,rest.y);
let calls='';const accessors={get x(){calls+='X';return 1;},get y(){calls+='Y';return 2;}};
const {x:readX,...accessorRest}=accessors;console.log(readX,accessorRest.y,calls);console.log(accessorRest.y,calls);
let coercions=0;const key={toString(){coercions++;return 'x';}};
const {[key as any]:selected,...computedRest}=source as any;console.log(selected,computedRest.y,coercions,Object.keys(computedRest).length);
const mutable:any={x:undefined,y:1};function fallback():number{mutable.y=8;mutable.z=9;return 7;}
const {x:defaulted=fallback(),...changed}=mutable;console.log(defaulted,changed.y,changed.z);
const {inner:{first,...inside},...outside}={inner:{first:1,second:2},third:3};console.log(first,inside.second,outside.third);
function parameter({first,...others}:{first:number;second:number}){return others.second+first;}
console.log(parameter({first:4,second:5}));
const array=[1,,3];const {0:initial,...arrayRest}=array;console.log(initial,arrayRest[2],Object.hasOwn(arrayRest,'1'),Object.keys(arrayRest).length);
const {length:arraySize,...elements}=[4,5];console.log(arraySize,elements[0],elements[1],Object.keys(elements).length);
const {0:character,...characters}='abc' as any;console.log(character,characters[1],characters[2],Object.keys(characters).length);
const {...empty}=3 as any;console.log(Object.keys(empty).length);
try{const {...invalid}=null as any;}catch(error){console.log(error instanceof TypeError);}
class RestClass {value=4;other=5;get inherited(){calls+='G';return 6;}}
const {inherited,...classRest}=new RestClass();console.log(inherited,classRest.value,classRest.other,Object.keys(classRest).length,calls);
const captured:(()=>number)[]=[];for(let index=0;index<3;index++){const {skip,...copy}={skip:0,value:index};captured.push(()=>copy.value);}
console.log(captured[0](),captured[1](),captured[2]());
let keyReads=0;const changingKey={value:'x',toString(){keyReads++;return this.value;}};
const changingSource:any={x:undefined,y:12};
const {[changingKey as any]:fallbackValue=(changingKey.value='y',13),...keySnapshot}=changingSource;
console.log(fallbackValue,keySnapshot.y,Object.hasOwn(keySnapshot,'x'),keyReads);
const protoSource={['__proto__']:7,a:8};const {a:skipProto,...protoCopy}=protoSource;console.log(protoCopy['__proto__'],Object.hasOwn(protoCopy,'__proto__'));
function restFunction(){}const functionValue:any=restFunction;functionValue.tag=9;
const {name:functionName,...functionRest}=functionValue;console.log(functionName,functionRest.tag,Object.hasOwn(functionRest,'length'),Object.keys(functionRest).length);
function genericRest<T extends {x:number}>(value:T){const {x,...others}=value;return others;}
console.log(genericRest({x:1,y:'yes'}).y);
function hoistedRest(){console.log(typeof copy);var {...copy}={value:14} as any;return copy.value;}
console.log(hoistedRest());
try{throw {x:1,y:15};}catch({x:ignored,...caught}:any){console.log(caught.y,Object.keys(caught).length);}

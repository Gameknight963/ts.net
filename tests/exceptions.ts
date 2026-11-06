let events='';
function result(flag:number):number {
 try {
  events+='T';
  if(flag===1)throw 'bad';
  if(flag===2)return 4;
  return 3;
 } catch(error) {events+='C'+error;return 5;}
 finally {events+='F';}
}
console.log(result(0),result(1),result(2),events);
function override():number {try{return 1;}finally{return 2;}}
console.log(override());
const thrown={x:7};
try{try{throw thrown;}finally{events+='N';}}catch(error){console.log(error===thrown);}
for(let i=0;i<4;i++){
 try{if(i===1)continue;if(i===2)break;events+='L';}finally{events+='E';}
}
console.log(events);
try{throw undefined;}catch(error){console.log(error===undefined);}
try{throw null;}catch(error){console.log(error===null);}
try{throw 42;}catch{console.log('optional');}
function replace():number{try{throw 1;}finally{return 9;}}
console.log(replace());
try{try{throw 'first';}catch(error){throw error;}finally{events+='Z';}}catch(error){console.log(error);}
const original=Error('cause');
const failure=new TypeError('broken',{cause:original});
console.log(failure.name,failure.message,failure.cause===original,failure instanceof Error,failure instanceof TypeError,failure instanceof RangeError);
console.log(''+failure,Object.keys(failure).length,Object.hasOwn(failure,'message'),typeof failure.stack);
try{throw failure;}catch(error){if(error instanceof Error)console.log(error===failure,error.name,error.message);}
const ErrorAlias=RangeError;const aliased=new ErrorAlias('range');console.log(aliased.name,aliased instanceof RangeError);
const noMessage=new Error();console.log(noMessage.message,Object.hasOwn(noMessage,'message'));
try{const missing:any=undefined;missing.field;}catch(error){console.log(error instanceof TypeError);}
function nestedReturns():number{try{try{return 1;}finally{events+='I';}}finally{events+='O';}}
console.log(nestedReturns(),events);
const handlers:(()=>unknown)[]=[];
for(let i=0;i<3;i++){try{throw i;}catch(error){handlers.push(()=>error);}}
console.log(handlers[0](),handlers[1](),handlers[2]());
try{throw {message:'pattern'};}catch({message}:any){console.log(message);}
function breakWins():number{for(let i=0;i<2;i++){try{return 1;}finally{break;}}return 8;}
console.log(breakWins());
let counter=0;
outer:while(counter<3){try{counter++;throw 'ignored';}finally{continue outer;}}
console.log(counter);
try{try{returningThrow();}finally{throw 'replacement';}}catch(error){console.log(error);}
function returningThrow():void{throw 'original';}
function nestedJump():number{let value=0;outer:for(let i=0;i<3;i++){try{for(let j=0;j<3;j++){try{if(j===1)continue outer;value++;}finally{value+=10;}}}finally{value+=100;}}return value;}
console.log(nestedJump());
const patterns:(()=>unknown)[]=[];
for(let i=0;i<3;i++){try{throw {value:i};}catch({value}:any){patterns.push(()=>value);}}
console.log(patterns[0](),patterns[1](),patterns[2]());
const constructorArgs:[string]=['spread'];console.log(new Error(...constructorArgs).message);
const group=new AggregateError([failure,original],'group',{cause:original});
console.log(group.name,group.message,group.errors[0]===failure,group.errors[1]===original,group instanceof Error,group instanceof AggregateError,Object.keys(group).length,AggregateError.length);
try{const values:any=[];values.length=-1;}catch(error){console.log(error instanceof RangeError);}
function temporalError():void{const read=()=>value;read();let value=3;}
try{temporalError();}catch(error){console.log(error instanceof ReferenceError);}
console.log(failure.toString());
const customError:any=new Error('before');
customError.name={toString:()=>{customError.message='after';return 'Custom';}};
console.log(customError.toString());
const genericErrorString={read:failure.toString,name:'Plain',message:'value'};
console.log(genericErrorString.read());

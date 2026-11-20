export {};
const events:string[]=[];
const prototype=new Proxy({}, {set(target,key,value,receiver){events.push('set:'+String(key)+':'+value);return Reflect.set(target,key,value,receiver);}});
const object:any=Object.create(prototype);object.value=7;console.log('created',object.value,Object.hasOwn(object,'value'),events.join('|'));
events.length=0;object.value=8;console.log('existing',object.value,events.join('|'));
function Item(this:any,value:number):void{this.value=value;}
const C:any=Item;C.prototype=prototype;events.length=0;const item=new C(11);console.log('constructed',item.value,Object.hasOwn(item,'value'),events.join('|'));

events.length=0;const middle=Object.create(prototype);const leaf:any=Object.create(middle);leaf.value=13;console.log('chain',leaf.value,events.join('|'));
events.length=0;const literal:any={__proto__:prototype};literal.value=17;console.log('literal',literal.value,events.join('|'));
let received:any;const absorbing=new Proxy({}, {set(target,key,value,receiver){received=receiver;events.push('absorb:'+String(key)+':'+value);return true;},getOwnPropertyDescriptor(){throw new Error('descriptor trap should not run');}});
const absorbed:any=Object.create(absorbing);events.length=0;absorbed.value=19;console.log('absorbed',received===absorbed,Object.hasOwn(absorbed,'value'),events.join('|'));
const rejecting=new Proxy({}, {set(){return false;}});const rejected:any=Object.create(rejecting);
try{rejected.value=23;}catch(error){console.log('rejected',(error as Error).name,Object.hasOwn(rejected,'value'));}
const throwing=new Proxy({}, {set(){throw new Error('set trap');}});try{(Object.create(throwing) as any).value=29;}catch(error){console.log('thrown',(error as Error).message);}
let calls=0;const counted=new Proxy({}, {set(target,key,value,receiver){calls++;return Reflect.set(target,key,value,receiver);}});
const fixed=Object.create(counted,{value:{value:31,writable:false}});try{fixed.value=37;}catch(error){console.log('own readonly',(error as Error).name,calls,fixed.value);}
let setterValue=0;const beforeProxy=Object.create(counted,{value:{set(value:number){setterValue=value;},configurable:true}});const setterChild:any=Object.create(beforeProxy);setterChild.value=41;console.log('setter before proxy',setterValue,calls);
const locked=Object.freeze({value:43});const invalid=new Proxy(locked,{set(){return true;}});try{(Object.create(invalid) as any).value=47;}catch(error){console.log('invariant',(error as Error).name);}
const revoked=Proxy.revocable({},{});const revokedChild:any=Object.create(revoked.proxy);revoked.revoke();try{revokedChild.value=53;}catch(error){console.log('revoked',(error as Error).name);}
events.length=0;const array:any[]=[];Object.setPrototypeOf(array,prototype);array[0]=59;console.log('array',array[0],array.length,events.join('|'));

events.length=0;const symbol=Symbol('key');object[symbol]=61;console.log('symbol',object[symbol],events.join('|'));
const inheritedFixed=Object.create(counted,{value:{value:67,writable:false}});const fixedChild:any=Object.create(inheritedFixed);try{fixedChild.value=71;}catch(error){console.log('inherited readonly',(error as Error).name,calls,Object.hasOwn(fixedChild,'value'));}
const inner=new Proxy({}, {set(target,key,value,receiver){events.push('inner:'+String(key));return Reflect.set(target,key,value,receiver);}});const outer=new Proxy(Object.create(inner),{});const nested:any=Object.create(outer);events.length=0;nested.value=73;console.log('nested proxy',nested.value,events.join('|'));
const functionProxy=new Proxy(function(){}, {set(target,key,value,receiver){events.push('function:'+String(key));return Reflect.set(target,key,value,receiver);}});const functionChild:any=Object.create(functionProxy);events.length=0;functionChild.value=79;console.log('function proxy',functionChild.value,events.join('|'));

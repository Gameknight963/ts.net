export {};
const events:string[]=[];
const prototype=new Proxy({}, {getPrototypeOf(){events.push('prototype trap');return null;}});
const Constructor:any=function(this:any){events.push('body');};
Constructor.prototype=prototype;
const value=new Constructor();
console.log('prototype',Object.getPrototypeOf(value)===prototype,events.join(','));
const Bound=Constructor.bind(null);
const bound=new Bound();
console.log('bound',Object.getPrototypeOf(bound)===prototype,events.join(','));
let cycle=false;
try{Object.setPrototypeOf(value,value);}catch{cycle=true;}
console.log('cycle rejected',cycle);
const Fallback:any=function(this:any){};
for(const candidate of [null,undefined,7,'text']) {
 Fallback.prototype=candidate;
 console.log('fallback',Object.getPrototypeOf(new Fallback())===Object.prototype);
}
let walks = 0;
const opaque = new Proxy({}, {getPrototypeOf() {walks++; throw new Error('prototype walk');}});
function Target() {}
(Target as any).prototype = opaque;
class Base {field = 7;}
class Derived extends Base {extra = 8;}
for (const Constructor of [Base, Derived, Array, Date, Map]) {
    try {
        const instance = Reflect.construct(Constructor, [], Target);
        console.log('construct opaque', Constructor.name, Object.getPrototypeOf(instance) === opaque);
    } catch {console.log('construct failed', Constructor.name);}
}
const ordinary = {};
try {Object.setPrototypeOf(ordinary, opaque);console.log('set opaque', Object.getPrototypeOf(ordinary) === opaque);}
catch {console.log('set failed');}
const reflected = {};
try {console.log('reflect opaque', Reflect.setPrototypeOf(reflected, opaque), Object.getPrototypeOf(reflected) === opaque);}
catch {console.log('reflect failed');}
console.log('walks', walks);
const revoked = Proxy.revocable({}, {});
revoked.revoke();
const another = {};
try {Object.setPrototypeOf(another, revoked.proxy);console.log('revoked prototype', Object.getPrototypeOf(another) === revoked.proxy);}
catch {console.log('revoked failed');}
const first = {}, second = Object.create(first);
console.log('ordinary cycle', Reflect.setPrototypeOf(first, second));
let rejected = false;
try {Object.setPrototypeOf(first, second);} catch {rejected = true;}
console.log('ordinary cycle throws', rejected);
const fixed = Object.preventExtensions(Object.create(opaque));
console.log('fixed prototype', Reflect.setPrototypeOf(fixed, opaque), Reflect.setPrototypeOf(fixed, null), walks);

class Stable {
    value: number;
    constructor(first = 1, second = 2) {this.value = first + second;}
}
const original = Stable.prototype;
console.log('class prototype fixed', Reflect.set(Stable, 'prototype', {}),
    Reflect.defineProperty(Stable, 'prototype', {value: {}}),
    Reflect.defineProperty(Stable, 'prototype', {get() {throw new Error('unreachable');}}));
console.log('class prototype unchanged', Stable.prototype === original,
    Object.getPrototypeOf(new Stable()) === original,
    Object.getPrototypeOf(new Stable(3)) === original,
    Object.getPrototypeOf(new Stable(3, 4)) === original);
console.log('class new target', Object.getPrototypeOf(Reflect.construct(Base, [], Stable)) === original);
let prototypeReads = 0;
const observed = new Proxy(Stable, {get(target, key, receiver) {
    if (key === 'prototype') prototypeReads++;
    return Reflect.get(target, key, receiver);
}});
console.log('proxy new target', Object.getPrototypeOf(new observed(4, 5)) === original, prototypeReads);

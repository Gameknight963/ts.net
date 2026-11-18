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

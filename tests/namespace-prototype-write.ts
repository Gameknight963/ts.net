import * as namespace from './optional-chaining-values.js';
function Empty():void {}
const Constructor:any=Empty;Constructor.prototype=namespace;
const instance=new Constructor();
for(const key of ['added','changing']) {
 let rejected=false;try{instance[key]=4;}catch(error){rejected=error instanceof TypeError;}
 if(!rejected)throw new Error('Namespace inherited writes must be rejected');
}
function Writes(this:any):void {this.added=1;}
const Blocked:any=Writes;Blocked.prototype=namespace;
let rejected=false;try{new Blocked();}catch(error){rejected=error instanceof TypeError;}
if(!rejected)throw new Error('Namespace prototype writes in a constructor must be rejected');
console.log('PASS namespace prototypes reject assignment and constructor writes');

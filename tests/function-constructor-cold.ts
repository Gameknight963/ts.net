function Cold(this:any):void {this.value=1;}
const Constructor:any=Cold;Constructor.prototype=null;
const instance=new Constructor();
console.log(typeof instance.constructor,instance.constructor===Object,instance.__proto__===Object.prototype);

const constructor = [].constructor as ArrayConstructor;
const prototype:any=constructor.prototype;
const saved=prototype.push;
prototype['push']=function(this:any,value:any):number {this[0]='text';return 1;};
function inferred():void {const values:any[]=[];values.push(1);console.log(values[0]);}
inferred();prototype.push=saved;

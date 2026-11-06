function Base(this:any) {this.value=1;}
Base.prototype.read=function(this:any):number{return this.value;};
(Base as any).tag=4;
class Derived extends (Base as any) {}
console.log(typeof Derived,Derived.tag,Derived.prototype.read===Base.prototype.read);

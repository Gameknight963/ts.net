function Typed(this:{value:number},value:number):void {this.value=value;}
const value=Reflect.construct(Typed,[9]);console.log(value.value,Object.getPrototypeOf(value)===Typed.prototype);
function Alternate():void {}
const changed=Reflect.construct(Typed,[10],Alternate);console.log(changed.value,Object.getPrototypeOf(changed)===Alternate.prototype);
const promise=Reflect.construct(Promise,[()=>{}],Alternate);console.log(Object.getPrototypeOf(promise)===Alternate.prototype);

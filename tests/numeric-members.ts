class Numeric {
 0=10;
 0x10='hex';
 0b11=30;
 0o10=80;
 1_000=1000;
 1.5='fraction';
 1e4='exponent';
 static 12=120;
 2(value:number):number{return this[0]+value;}
 get 4():number{return this[0]*2;}
 set 4(value:number){this[0]=value/2;}
 accessor 5=50;
}
const value=new Numeric();
console.log('fields',value[0],value[16],value[3],value[8],value[1000],value[1.5],value[10000],Numeric[12]);
console.log('method',value[2](3),value[2].name,value[4],value[5]);value[4]=40;value[5]=60;console.log('updated',value[0],value[4],value[5]);
console.log('keys',Object.keys(value).join(','));
console.log('method own',Object.hasOwn(value,'2'),Object.hasOwn(value,'5'));
const literal={0:()=>1,0x20:function(){return 2;},1e3(){return 3;}};console.log('literal names',literal[0].name,literal[32].name,literal[1000].name);
class Derived extends Numeric {6=6;}
console.log('derived',new Derived()[0],new Derived()[6]);
class Indexed {0=10;2=30;length=3;}
const indexed=new Indexed();(Array.prototype.copyWithin as any).call(indexed,2,1,2);console.log('indexed class',indexed[0],indexed[2],Object.keys(indexed).join(','));
class Normalized {1e21='large';1e-7='small';0xFFFF_FFFF='limit';static 0x20='static';}
const normalized=new Normalized();console.log('normalized',normalized[1e21],normalized[1e-7],normalized[4294967295],Normalized[32],Object.keys(normalized).join(','));
class SetterOnly {private stored=0;set 0x10(value:number){this.stored=value;}read():number{return this.stored;}}
const setter=new SetterOnly();setter[16]=7;console.log('numeric setter',setter.read());
const assigned:{'0':number,'16':string}=new Numeric();console.log('structural class',assigned[0],assigned[16]);

import {Imported} from './class-values';
class ImportedChild extends Imported {override read():number{return super.read()+1;}}
const child=new ImportedChild(20);console.log(child.read(),child instanceof Imported,child instanceof ImportedChild);
function parentFactory(value:number){return class {value=value;read(){return this.value;}};}
const Parent=parentFactory(6);
class CapturedChild extends Parent {override read(){return super.read()+2;}}
console.log(new CapturedChild().read());

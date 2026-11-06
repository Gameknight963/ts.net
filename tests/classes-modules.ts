import DefaultClass,{Imported} from './class-values';
const first=new DefaultClass();const second=new Imported();
console.log(DefaultClass.value,first.read(),second.read(),first instanceof DefaultClass,second instanceof Imported);

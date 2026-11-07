import {wrap,replace} from './decorator-module.js';
@replace
export class Example {
 value=7;
 @wrap method() {return this.value;}
}
const value=new Example(); console.log(value.method(),(value as any).extra);

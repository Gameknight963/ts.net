export function tag(parts:TemplateStringsArray,...values:any[]):string {return parts.join("|")+":"+values.join("|");}
export function site():TemplateStringsArray{return capture`module`;}
function capture(parts:TemplateStringsArray):TemplateStringsArray{return parts;}
export const object={prefix:"module",tag(parts:TemplateStringsArray,value:number):string{return this.prefix+":"+parts[0]+value;}};

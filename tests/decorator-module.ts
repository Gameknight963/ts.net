export function wrap(value: any, context: ClassMethodDecoratorContext<any, any>) {
 context.addInitializer(function(this:any) {console.log('ready',context.name,this.value);});
 return function(this:any,...args:any[]) {return value.apply(this,args)+1;};
}
export function replace(value:any):any {return class extends value {extra=3;};}

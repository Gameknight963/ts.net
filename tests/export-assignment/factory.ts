function factory(value:number){return {value,read(){return this.value+1;}};}
namespace factory {export const label="factory";export function twice(value:number){return value*2;}}
export = factory;

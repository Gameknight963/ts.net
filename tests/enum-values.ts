export enum Exported {A=2,B=4}
export enum Exported {C=8}
export const enum Inlined {Value=Exported.B+1,Text="text"}
export namespace Exported {
 export let state=3;
 let privateCount=0;
 export function next():number{return ++privateCount+state+Exported.A;}
}
export namespace Exported {
 export namespace Nested {
  export enum Choice { First=3, Last=6 }
  export let value=10;
  export function read():number { return value+Choice.Last; }
  export class Box { value:number; constructor(){this.value=read();} }
 }
}

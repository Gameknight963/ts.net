export enum Exported {A=2,B=4}
export enum Exported {C=8}
export const enum Inlined {Value=Exported.B+1,Text="text"}
export namespace Exported {
 export let state=3;
 let privateCount=0;
 export function next():number{return ++privateCount+state+Exported.A;}
}

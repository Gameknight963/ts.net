export enum Exported {A=2,B=4}
export enum Exported {C=8}
export const enum Inlined {Value=Exported.B+1,Text="text"}

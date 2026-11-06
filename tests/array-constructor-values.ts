export const constructor: ArrayConstructor = Array;
export class Values extends Array<number> {sum():number {return this.reduce((a,b)=>a+b,0);}}
export function create():number[] {return new Array<number>(1,2,3);}

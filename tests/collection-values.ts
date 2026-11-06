export const map=new Map<number,string>([[1,'one']]);
export const set=new Set([2,3]);
export const MapAlias=Map;
export function change():void{map.set(2,'two');set.add(4);}
export class Table extends Map<number,number>{label='table';}

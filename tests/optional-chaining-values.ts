export const service={value:12,read(this:{value:number},increment:number):number{return this.value+increment;}};
export function maybe(value:number):number{return value*2;}
export let changing=1;
export function update():void {changing++;}
import * as own from './optional-chaining-values.js';
export function earlyReceiver():boolean{return own.receiver();}
export function receiver(this:any):boolean{return this===undefined;}
export default function defaultValue():number{return 23;}

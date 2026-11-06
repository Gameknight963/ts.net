export const promise:Promise<number>=Promise.resolve(10);
export function increment(value:number):number {return value+1;}
export function create():Promise<number> {return new Promise(resolve=>resolve(4));}

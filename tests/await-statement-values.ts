export async function work(value:number):Promise<number>{let result=await value;result+=await 1;return result;}
export async function branch(value:boolean):Promise<string>{if(await value){await 0;return 'yes';}return 'no';}
export async function pattern():Promise<number>{const {x=await 4,...rest}=await Promise.resolve({x:undefined,y:5});return x+rest.y;}
export async function choose(value:number):Promise<string>{switch(await value){case await 1:await 0;return 'one';default:await 0;break;}return 'other';}

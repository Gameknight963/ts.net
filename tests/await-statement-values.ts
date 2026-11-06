export async function work(value:number):Promise<number>{let result=await value;result+=await 1;return result;}
export async function branch(value:boolean):Promise<string>{if(await value){await 0;return 'yes';}return 'no';}

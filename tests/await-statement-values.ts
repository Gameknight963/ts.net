export async function work(value:number):Promise<number>{let result=await value;result+=await 1;return result;}
export async function branch(value:boolean):Promise<string>{if(await value){await 0;return 'yes';}return 'no';}
export async function pattern():Promise<number>{const {x=await 4,...rest}=await Promise.resolve({x:undefined,y:5});return x+rest.y;}
export async function choose(value:number):Promise<string>{switch(await value){case await 1:await 0;return 'one';default:await 0;break;}return 'other';}
export async function loop():Promise<number>{let total=0;for(const value of await [1,2,3])total+=await value;for(let i=0;await(i<2);i++)total+=i;return total;}
export async function protectedValue():Promise<number>{try{return await Promise.reject(8);}catch(error){return await (error as number)+1;}finally{await 0;console.log('module finally');}}

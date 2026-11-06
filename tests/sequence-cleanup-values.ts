export function first():number {for(const value of [1,2,3])return value;return 0;}
export async function awaited():Promise<number>{for await(const value of [4,5,6])return value;return 0;}

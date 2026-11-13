export function objectOperation(name:string):any {return (Object as any)[name];}
export const freeze=objectOperation('freeze');

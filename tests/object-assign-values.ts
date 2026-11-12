export function create<T extends object>(source:T):T{return Object.assign({},source);}
export function merge<T extends object,U>(target:T,source:U):T&U{return Object.assign(target,source);}
export const assign=Object.assign;
export const tools={copy:Object.assign};

export function copy<T>(values:ArrayLike<T>):T[]{return Array.from(values);}
export function mapped(values:number[]):number[]{const offset=3;return Array.from(values,(value,index)=>value+offset+index);}

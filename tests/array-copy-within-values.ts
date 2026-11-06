export const record={0:1,2:3,length:3,label:'module'};
export function move():typeof record{(Array.prototype.copyWithin as any).call(record,1,0,2);return record;}
export function moveArray<T>(values:T[]):T[]{return values.copyWithin(1,0,1);}

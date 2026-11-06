export function format(value: string): string {return value.trim().padStart(5, '0').repeat(2);}
export const repeat = String.prototype.repeat;

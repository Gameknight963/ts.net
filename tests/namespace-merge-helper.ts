export function Counter(n: number) {return n + Counter.offset;}
export namespace Counter {export let offset = 2; export const read = () => offset;}
export class Box {constructor(public value: number) {}}
export namespace Box {export const create = (n: number) => new Box(n); export namespace Nested {export const value = 7;}}

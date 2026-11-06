export function collect(): IArguments { return arguments; }
export function capture(value: number): () => IArguments { return () => arguments; }
export class Parent {
    read: () => IArguments;
    constructor(value: number) { this.read = () => arguments; }
}

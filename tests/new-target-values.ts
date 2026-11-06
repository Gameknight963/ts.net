export function Constructor(this: any): void { this.target = new.target; this.read = () => new.target; }
export class Parent {
    target: unknown;
    constructor() { this.target = new.target; }
}

export const Constructor = Object;
export const Text = String;
export const Truth = Boolean;
export class Parent extends Object {
    target: unknown;
    constructor(value: object) {super(value); this.target = new.target;}
}

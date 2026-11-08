export {};
function Target(this: any, first: number, second: number): void {
    this.values = [first, second];
    this.target = new.target;
}
const Bound: any = Target.bind({ignored: true}, 1);
const Again: any = Bound.bind(null, 2);
const first = new Bound(3), second = new Again();
console.log('ordinary', first.values.join(','), first.target === Target, first instanceof Bound, first instanceof Target);
console.log('nested', second.values.join(','), second.target === Target, second instanceof Again, second instanceof Bound);
function Other(): void {}
const reflected = Reflect.construct(Bound, [4], Other);
console.log('reflect', reflected.values.join(','), reflected.target === Other, Object.getPrototypeOf(reflected) === Other.prototype, reflected instanceof Target);
class Value {
    target: unknown;
    constructor(public value: number) {this.target = new.target;}
}
const BoundClass: any = Value.bind(null, 5);
const instance = new BoundClass();
console.log('class', instance.value, instance.target === Value, instance instanceof Value);
const alternate = Reflect.construct(BoundClass, [], Other);
console.log('class reflect', alternate.value, alternate.target === Other, Object.getPrototypeOf(alternate) === Other.prototype);
function Replacement(this: any): any {return {target: new.target, replacement: true};}
const BoundReplacement: any = Replacement.bind(null);
const replacement = new BoundReplacement();
console.log('replacement', replacement.target === Replacement, replacement.replacement, replacement instanceof Replacement);

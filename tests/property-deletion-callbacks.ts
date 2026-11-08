export {};
type Remove = typeof Reflect.deleteProperty;
function apply(target: object, operation: Remove): boolean {return operation(target, 'a');}
const first = {a: 1};
console.log('callback', apply(first, Reflect.deleteProperty), first.a);
const retained = {a: 2};
console.log('custom', apply(retained, () => false), retained.a);

function forward(target: object, operation: Remove): boolean {return apply(target, operation);}
const second = {a: 3};
console.log('forward', forward(second, Reflect.deleteProperty), second.a);

const arrow = (target: object, operation: Remove): boolean => forward(target, operation);
const third = {a: 4};
console.log('arrow', arrow(third, Reflect.deleteProperty), third.a);

function namespace(target: object, reflection: typeof Reflect): boolean {return reflection.deleteProperty(target, 'a');}
const fourth = {a: 5};
console.log('namespace', namespace(fourth, Reflect), fourth.a);

function destructured(target: object, {deleteProperty: remove}: typeof Reflect): boolean {return remove(target, 'a');}
const fifth = {a: 6};
console.log('destructured', destructured(fifth, Reflect), fifth.a);

function bind(target: object, operation: Remove): () => boolean {return operation.bind(null, target, 'a');}
const sixth = {a: 7};
console.log('bind', bind(sixth, Reflect.deleteProperty)(), sixth.a);

function recursive(target: object, operation: Remove, depth: number): boolean {
    return depth ? recursive(target, operation, depth - 1) : operation(target, 'a');
}
const seventh = {a: 8};
console.log('recursive', recursive(seventh, Reflect.deleteProperty, 3), seventh.a);

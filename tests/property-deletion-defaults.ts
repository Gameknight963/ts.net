export {};

function remove(target: object, key: PropertyKey, operation = Reflect.deleteProperty): boolean {
    return operation(target, key);
}
const deleted = {a: 1, b: 2};
console.log('delete default', remove(deleted, 'a'), deleted.a, Object.keys(deleted).join(','));
const retained = {a: 3};
console.log('delete override', remove(retained, 'a', () => false), retained.a);

function deleteNamespace(target: object, namespace = Reflect): boolean {
    return namespace.deleteProperty(target, 'a');
}
const namespaced = {a: 11};
console.log('reflect namespace', deleteNamespace(namespaced), namespaced.a);

function destructuredDelete(target: object, {deleteProperty: operation} = Reflect): boolean {
    return operation(target, 'a');
}
const destructured = {a: 14};
console.log('destructured', destructuredDelete(destructured), destructured.a);

function bindDefault(target: object, operation = Reflect.deleteProperty): () => boolean {
    return operation.bind(null, target, 'a');
}
const bound = {a: 15};
console.log('bound default', bindDefault(bound)(), bound.a);


export {};
function remove(useFirst: boolean) {
    const erase = useFirst ? Reflect.deleteProperty : Reflect.deleteProperty.bind(Reflect);
    const target: any = {conditionalField: 7, retained: 8};
    console.log('removed', useFirst, erase(target, 'conditionalField'), 'conditionalField' in target, Object.keys(target).join(','), target.retained);
}
remove(true);
remove(false);

function namespace(remove: boolean) {
    const alias = remove ? Reflect : {deleteProperty: (_target: object, _key: PropertyKey) => false};
    const target = {namespaceField: 1, other: 2};
    console.log('namespace', remove, alias.deleteProperty(target, 'namespaceField'), Object.keys(target).join(','));
}
namespace(true);
namespace(false);

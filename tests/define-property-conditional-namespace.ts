export {};
function define(useObject: boolean) {
    const api = useObject ? Object : Reflect;
    const value = {conditionalDefinition: 1, retained: 2};
    const result = api.defineProperty(value, 'conditionalDefinition', {value: 9, enumerable: false});
    console.log('define', useObject, result === value, result === true, value.conditionalDefinition, Object.keys(value).join(','));
    console.log('descriptor', JSON.stringify(Object.getOwnPropertyDescriptor(value, 'conditionalDefinition')));
}
define(true);
define(false);

function prevent(useObject: boolean) {
    const api = useObject ? Object : Reflect;
    const value = {conditionalIntegrity: 3};
    api.preventExtensions(value);
    console.log('integrity', useObject, Object.isExtensible(value), value.conditionalIntegrity);
    try {(value as any).added = 4;} catch (error) {console.log('extension', (error as Error).name);}
}
prevent(true);
prevent(false);
function borrowed(useObject: boolean) {
    const api = useObject ? Object : Reflect;
    const method = api.defineProperty;
    const value = {conditionalBorrowed: 4};
    method.call(api, value, 'conditionalBorrowed', {value: 8, writable: false});
    console.log('borrowed', useObject, value.conditionalBorrowed, Object.getOwnPropertyDescriptor(value, 'conditionalBorrowed')!.writable);
}
borrowed(true);
borrowed(false);

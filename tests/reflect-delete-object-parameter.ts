export {};
function remove(value: object, key: string): boolean {return Reflect.deleteProperty(value, key);}
function removeUnknown(value: any, key: string): boolean {return Reflect.deleteProperty(value, key);}
const value = {first: 1, second: 2};
console.log('object parameter', remove(value, 'first'), Reflect.has(value, 'first'), Object.keys(value).join(','));
value.first = 3;
console.log('restore', value.first, Object.keys(value).join(','));
const other = {label: 'value', remaining: true};
console.log('any parameter', removeUnknown(other, 'label'), Reflect.get(other, 'label'), Object.keys(other).join(','));
function removeGeneric<T extends object>(value: T, key: string): boolean {return Reflect.deleteProperty(value, key);}
const generic = {count: 4, name: 'generic'};
console.log('generic', removeGeneric(generic, 'count'), Object.keys(generic).join(','));
const dispatch = Reflect.deleteProperty;
function aliased(value: object, key: string): boolean {return dispatch(value, key);}
const alias = {removed: 5, retained: 6};
console.log('alias', aliased(alias, 'removed'), Reflect.get(alias, 'removed'), Object.keys(alias).join(','));

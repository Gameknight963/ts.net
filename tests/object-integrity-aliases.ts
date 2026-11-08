export {};
const freeze = true;
const operation = freeze ? Object.freeze : Object.seal;
const value = {conditionalField: 1};
operation(value);
try {value.conditionalField = 2;} catch (error) {console.log('conditional', (error as Error).name);}
console.log('frozen', Object.isFrozen(value), value.conditionalField);
const {seal} = Object;
const other = {sealedField: 3};
seal(other);
other.sealedField = 4;
console.log('sealed', Object.isSealed(other), Object.isFrozen(other), other.sealedField, Reflect.deleteProperty(other, 'sealedField'));

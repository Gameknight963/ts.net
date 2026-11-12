export {};
const tools = {copy: Object.assign};
const source = {throughProperty: 1};
console.log('property copy', tools.copy({}, source).throughProperty);
const alias = tools.copy;
console.log('property function alias', alias({}, {throughAlias: 2}).throughAlias);
const holderAlias = tools;
console.log('holder alias', holderAlias.copy({}, {throughHolderAlias: 3}).throughHolderAlias);
const named = {assign: Object.assign};
console.log('named assign property', named.assign({}, {throughNamedProperty: 4}).throughNamedProperty);
console.log('indexed copy property', tools['copy']({}, {throughIndexedProperty: 5}).throughIndexedProperty);
const asserted = tools.copy as typeof Object.assign;
console.log('asserted copy alias', asserted({}, {throughAssertedAlias: 6}).throughAssertedAlias);

let propertyReads = 0;
const getterTools = {get copy() {propertyReads++; return Object.assign;}};
console.log('getter copy', getterTools.copy({}, {throughGetter: 7}).throughGetter, propertyReads);
const getterAlias = getterTools.copy;
console.log('getter function alias', getterAlias({}, {throughGetterAlias: 8}).throughGetterAlias, propertyReads);
let setterCalls = 0;
const paired = {
    get copy() {propertyReads++; return Object.assign;},
    set copy(value: typeof Object.assign) {setterCalls++;}
};
console.log('paired accessor copy', paired.copy({}, {throughPair: 9}).throughPair, propertyReads, setterCalls);
const unrelated = {copy: Object.assign, get unused(): number {throw new Error('unexpected getter');}};
console.log('unrelated getter', unrelated.copy({}, {throughUnrelated: 10}).throughUnrelated);

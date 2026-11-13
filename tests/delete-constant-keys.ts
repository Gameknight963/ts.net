const deleteKey = 'first';
const deleteAlias = deleteKey;
const constantDeleteTarget: any = {first: 1, second: 2};
console.log('constant', delete constantDeleteTarget[deleteAlias], Object.keys(constantDeleteTarget).join(','), constantDeleteTarget.first);
constantDeleteTarget.first = 3;
console.log('reinsert', Object.keys(constantDeleteTarget).join(','), constantDeleteTarget.first);
const assertedDeleteKey = 'first' as 'second';
const assertedDeleteTarget: any = {first: 4, second: 5};
console.log('actual-value', delete assertedDeleteTarget[assertedDeleteKey], Object.keys(assertedDeleteTarget).join(','));
let mutableDeleteKey = 'first';
mutableDeleteKey = 'second';
const mutableDeleteTarget: any = {first: 6, second: 7};
console.log('mutable', delete mutableDeleteTarget[mutableDeleteKey], Object.keys(mutableDeleteTarget).join(','));
const {key: destructuredDeleteKey} = {key: 'second'};
const destructuredDeleteTarget: any = {first: 8, second: 9};
console.log('destructured', delete destructuredDeleteTarget[destructuredDeleteKey], Object.keys(destructuredDeleteTarget).join(','));
function deleteWithShadowedKey(): void {
    const deleteKey = 'second';
    const target: any = {first: 10, second: 11};
    console.log('shadowed', delete target[deleteKey], Object.keys(target).join(','));
}
deleteWithShadowedKey();

import {exportedDeleteAlias as importedKey, changingDeleteKey, changeDeleteKey} from './delete-constant-key-library.js';
const importedDeleteTarget: any = {first: 1, second: 2};
console.log('imported', delete importedDeleteTarget[importedKey], Object.keys(importedDeleteTarget).join(','));
changeDeleteKey();
const liveDeleteTarget: any = {first: 3, second: 4};
console.log('live', delete liveDeleteTarget[changingDeleteKey], Object.keys(liveDeleteTarget).join(','));

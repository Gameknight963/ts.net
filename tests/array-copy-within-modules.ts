import {record,move,moveArray} from './array-copy-within-values';
console.log('module identity',move()===record,record[0],record[2],Object.keys(record).join(','));
console.log('generic array',moveArray(['a','b']).join(','));

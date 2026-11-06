import {map,set,MapAlias,change,Table} from './collection-values';
console.log('before',map.size,set.size);change();
for(const [key,value] of map)console.log('module map',key,value);
console.log('module set',Array.from(set).join(','));
const copied=new MapAlias(map);console.log('alias',copied.get(2),copied instanceof Map);
const table=new Table([[2,5]]);console.log('subclass',table.label,table.get(2),table instanceof Table);
function shadow():number{class Map{value=7;}return new Map().value;}
console.log('shadow',shadow());
async function run():Promise<number>{let total=0;for await(const value of set)total+=value;return total;}
run().then(value=>console.log('module async',value));

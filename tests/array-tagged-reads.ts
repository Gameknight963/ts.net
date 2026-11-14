export {};

const token = {tag: 'object'};
const values: any[] = [1, 'text', -0, NaN, Infinity, undefined, null, , token];
console.log('numeric tag', typeof values[0] === 'number');
const alias: any = values;
console.log('ordinary reads', alias[0], alias[0], alias[1], Object.is(alias[2], -0), Number.isNaN(alias[3]), alias[4]);
console.log('other reads', alias[5], alias[6], 7 in alias, alias[8] === token);
values[0] = 7;
console.log('rewritten number', alias[0]);
values[0] = 'changed';
console.log('rewritten reference', alias[0]);
values[0] = -0;
console.log('rewritten negative zero', Object.is(alias[0], -0));
delete values[0];
console.log('deleted cached value', 0 in values, alias[0]);
values[0] = 12;
Object.defineProperty(values, '0', {get() {return 23;}, configurable: true});
console.log('defined cached index', alias[0]);
delete values[0];
values[0] = 34;
values.length = 2;
values.length = 4;
console.log('truncated cached values', alias[0], alias[2], 2 in values);

const frozen: any[] = [3, 'frozen'];
console.log('frozen numeric tag', typeof frozen[0] === 'number');
Object.freeze(frozen);
console.log('frozen reads', frozen[0], frozen[0], frozen[1]);
try {frozen[0] = 9;} catch {console.log('frozen write rejected');}

const copied: any[] = [5, 'copy', 6];
console.log('copied numeric tag', typeof copied[0] === 'number', copied[0]);
copied.reverse();
console.log('materialized reads', copied.join(','), copied[2]);

const changing: any[] = [1, , 3];
console.log('changing numeric tag', typeof changing[0] === 'number');
const changingPrototype = Object.create(Array.prototype);
Object.defineProperty(changingPrototype, '1', {get() {
    changing[1000] = 7;
    return changing[0];
}});
Object.setPrototypeOf(changing, changingPrototype);
console.log('getter changes storage', changing[1], changing[0], changing[1000], changing.length);

function readThroughHelper(items:any[], index:number):any { return items[index]; }
const helperValues:any[]=[7,"helper",,undefined];
console.log("helper initial",readThroughHelper(helperValues,0),readThroughHelper(helperValues,1),readThroughHelper(helperValues,2),readThroughHelper(helperValues,3));
helperValues[0]=13;
console.log("helper updated",readThroughHelper(helperValues,0));
Object.defineProperty(helperValues,"0",{get(){return "getter";},configurable:true});
console.log("helper getter",readThroughHelper(helperValues,0));
delete helperValues[0];
const helperPrototype=Object.create(Array.prototype);
Object.defineProperty(helperPrototype,"0",{get(){return 29;}});
Object.setPrototypeOf(helperValues,helperPrototype);
console.log("helper inherited",readThroughHelper(helperValues,0));
helperValues.length=1;
console.log("helper truncated",readThroughHelper(helperValues,1));

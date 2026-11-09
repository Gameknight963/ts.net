const values = [1, 2, 3];
console.log('numeric order',values.reduce((acc,value)=>acc*10+value,0),values.reduceRight((acc,value)=>acc*10+value,0));
function observed(acc:number,value:number):number{return acc+value+arguments.length;}
console.log('argument count',values.reduce(observed,0));
console.log('variadic native',values.reduce(Math.max as (acc:number,value:number)=>number,0));
console.log('default accumulator',values.reduce<number|undefined>((acc=5,value)=>acc+value,undefined));
console.log('default value',[1,undefined,3].reduce<number>((acc,value=8)=>acc+value,0));
const changed=[1,2,3];let changes=0;
console.log('numeric mutation',changed.reduce((acc,value)=>{if(changes++===0){changed[1]=7;changed.push(9);}return acc*10+value;},0),changes,changed.length);
console.log('sum', values.reduce((sum, value) => sum + value), values.reduce((sum, value) => sum + value, 10));
console.log('right', values.reduceRight((sum, value) => sum - value), values.reduceRight((sum, value) => sum - value, 10));
console.log('generic-type', values.reduce((text, value) => text + value, 'start:'));
const reduce: any = values.reduce, reduceRight: any = values.reduceRight;
console.log('metadata', reduce.name, reduce.length, reduceRight.name, reduceRight.length);
console.log('empty-initial', reduce.call([], () => 99, undefined) === undefined, reduce.call([], () => 99, null) === null);
for (const method of [reduce, reduceRight]) {
    for (const array of [[], [, ,]]) {
        try {method.call(array, () => 0);} catch(error) {console.log('empty', error instanceof TypeError);}
    }
    try {method.call([], undefined, 0);} catch(error) {console.log('bad-callback', error instanceof TypeError);}
    try {method.call(null, () => 0);} catch(error) {console.log('bad-receiver', error instanceof TypeError);}
}
const sparse: number[] = []; sparse.length = 5; sparse[1] = 2; sparse[3] = 4;
let visited = '';
console.log('sparse', sparse.reduce((sum, value, index) => {visited += index;return sum + value;}), visited);
visited = '';
console.log('sparse-right', sparse.reduceRight((sum, value, index) => {visited += index;return sum - value;}), visited);
let calls = 0;
console.log('single', reduce.call([undefined], () => calls++) === undefined, calls);
console.log('explicit-undefined', reduce.call([7], (sum: unknown, value: number) => sum === undefined ? value : 0, undefined));
function callback(this: unknown, sum: number, value: number, index: number, source: number[]): number {
    console.log('callback', arguments.length, this === undefined, source === values, index);
    return sum + value;
}
console.log('callback-result', values.reduce(callback, 0));
const growing = [1, 2, 3];
console.log('growth', growing.reduce((sum, value, index) => {if(index === 0) {growing.push(4);growing[1] = 8;}return sum + value;}, 0));
const shrinking = [1, 2, 3];
console.log('shrink', shrinking.reduce((sum, value, index) => {if(index === 0) shrinking.length = 1;return sum + value;}, 0));
const rightMutation = [1, 2, 3];
console.log('right-mutation', rightMutation.reduceRight((sum, value, index) => {if(index === 2) {rightMutation[0] = 9;rightMutation.push(4);}return sum + value;}, 0));
const object = {0: 'a', 2: 'c', length: 3};
console.log('object', reduce.call(object, (text: string, value: string, index: number) => text + index + value, ''), reduceRight.call(object, (text: string, value: string) => text + value));
const inherited: any = {length: 3}; inherited.__proto__ = {1: 8};
console.log('inherited', reduce.call(inherited, () => 0), reduceRight.call(inherited, (sum: number, value: number) => sum + value, 1));
const array: any[] = []; array.length = 3; (array as any).__proto__ = {1: 9};
console.log('inherited-array', reduce.call(array, () => 0));
console.log('string', reduce.call('abc', (text: string, value: string, index: number, source: any) => text + index + value + typeof source, ''), reduceRight.call('abc', (text: string, value: string) => text + value));
function argumentsSource(): number {return reduce.call(arguments, (sum: number, value: number) => sum + value, 0);}
console.log('arguments', (argumentsSource as any)(4, 5, 6));
let effects = '';
const accessors = {get length(): number {effects += 'L';return 3;}, get 0(): number {effects += 'A';return 1;}, get 2(): number {effects += 'C';return 3;}};
console.log('getters', reduce.call(accessors, (sum: number, value: number) => {effects += 'B';return sum + value;}), effects);
effects = '';
try {reduce.call({get length(): number {effects += 'L';return 0;}}, null);} catch(error) {console.log('order', effects, error instanceof TypeError);}
try {values.reduce(() => {throw 'failure';});} catch(error) {console.log('thrown', error);}
const initial = {total: 0};
const result = values.reduce((state, value) => {state.total += value;return state;}, initial);
console.log('object-accumulator', result === initial, result.total);

const holes: (number | undefined)[] = [1, , 3];
console.log(holes.length, holes[0], holes[1], holes[9], holes[1] === undefined);
const numbers: number[] = [1, 2, 3];
numbers[1] = 2;
console.log(numbers[1]++, ++numbers[1], numbers[1] += 5);
console.log(numbers.push(10), numbers.push(11, 12), numbers.push());
console.log(numbers.pop(), numbers.length, numbers[2]);
numbers.length = 1;
console.log(numbers.length, numbers[1], numbers.pop(), numbers.pop());
numbers.length = 4;
console.log(numbers.length, numbers[0], numbers[3]);
numbers[3] = 8;
console.log(numbers.pop(), numbers[3], numbers.length);
const alias = numbers;
alias[0] = 7;
console.log(numbers[0], alias === numbers);
function sum(values: number[]) { return values[0] + values[1]; }
console.log(sum([3, 4]));
function generic<T>(values: T[]) { return values[0]; }
console.log(generic([9]), generic(["text"]));
const text: string[] = ["a", "b"];
text[0] += "!";
console.log(text[0], text[0].length, text[9], typeof text[9], text[9] === undefined);
console.log(text.pop(), text.pop(), text.pop());
const flags: boolean[] = [true, false];
console.log(flags[0], flags[1], flags[7], flags[7] === undefined);
type Point = { x: number };
const points: Point[] = [{ x: 3 }];
console.log(points[0].x, points[7], points[7] === undefined);
points[0].x++;
console.log(points.pop()!.x, points.pop());
const nested: number[][] = [[1, 2], [3]];
console.log(nested[0][1], nested[1][0], nested[5]);
const tuple: [number, string] = [7, "seven"];
console.log(tuple[0], tuple[1]);
const readonlyValues: readonly number[] = [4, 5];
console.log(readonlyValues.length, readonlyValues[0]);

const sparse: number[] = [];
sparse[100] = 9;
for (let i = 0; i < 100; i++) sparse[i] = i;
console.log(sparse[100], sparse.length, sparse[99]);
sparse[4294967294] = 11;
console.log(sparse.length, sparse[4294967294], sparse[4294967293]);
sparse.length = 3;
console.log(sparse.length, sparse[100], sparse[4294967294]);
const dynamic: any = sparse;
dynamic[-1] = "negative";
dynamic[1.5] = "fraction";
dynamic["01"] = "leading";
dynamic["-0"] = "minus zero";
dynamic[4294967295] = "extra";
console.log(dynamic[-1], dynamic[1.5], dynamic["01"], dynamic["-0"], dynamic[4294967295], sparse.length);
dynamic["0"] = 12;
console.log(sparse[0], dynamic["length"]);
console.log(dynamic.push(14), dynamic.pop(), sparse.length);
dynamic.length = "1";
console.log(sparse.length, sparse[1]);
const genericArray: any[] = [3, 4];
const numericView: number[] = genericArray;
numericView[0]++;
console.log(numericView[0], numericView.push(8), genericArray[2]);
console.log("hello"[1], "hello"[20]);

function receiver(values: number[], counter: Point) { counter.x++; return values; }
function index(counter: Point) { counter.x++; return 0; }
function right(values: number[]) { values[0] = 100; return 3; }
const ordered = [7];
const calls = { x: 0 };
console.log(receiver(ordered, calls)[index(calls)] += right(ordered), ordered[0], calls.x);
function argument(values: number[]) { values.length = 0; return 5; }
console.log(ordered.push(ordered[0], argument(ordered)), ordered[0], ordered[1]);
function defaults(values: number[] = [1, 2]) { values[0]++; return values[0]; }
console.log(defaults(), defaults());
const special: (number | undefined)[] = [0 / 0, -0, 1 / 0, undefined];
console.log(special[0], special[0] !== special[0], special[0] === undefined);
console.log(1 / special[1]!, special[2], special[3], typeof special[3]);
{
    const ordinary=Array.from([1,2,3],value=>value*2);
    ordinary[-1]=7;ordinary[1.5]=8;ordinary[NaN]=9;ordinary[Infinity]=10;
    ordinary[4294967294]=11;ordinary[4294967295]=12;
    console.log('ordinary numeric keys',ordinary[0],ordinary[-0],ordinary[-1],ordinary[1.5],ordinary[NaN],ordinary[Infinity],ordinary[4294967294],ordinary[4294967295]);
    let reads=0;
    Object.defineProperty(ordinary,'0',{get(){reads++;return 13;},configurable:true});
    console.log('ordinary numeric getter',ordinary[0],reads);
    delete ordinary[1];let inherited=0,value=0;
    Object.defineProperty(Array.prototype,'1',{get(){inherited++;return 14;},configurable:true});
    try{value=ordinary[1];}finally{delete (Array.prototype as any)[1];}
    console.log('ordinary inherited numeric getter',value,inherited);
}

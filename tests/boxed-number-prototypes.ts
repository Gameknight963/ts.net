export {};
const number = new Number(7);
const original = Object.getPrototypeOf(number);
console.log('normal', typeof number.valueOf, number.valueOf());
const empty = Object.create(null);
Object.setPrototypeOf(number, empty);
console.log('missing', typeof number.valueOf, typeof number.toString, 'valueOf' in number, 'toString' in number);
empty.valueOf = () => 9;
console.log('custom', number.valueOf(), 'valueOf' in number);
delete empty.valueOf;
console.log('deleted', typeof number.valueOf, 'valueOf' in number);
Object.setPrototypeOf(number, original);
console.log('restored', number.valueOf(), number.toString());

const separateNumberLeft: any = Object(1);
const separateNumberRight: any = Object(1);
separateNumberLeft.extra = 'left';
console.log('boxed identity', separateNumberLeft === separateNumberRight, separateNumberLeft.valueOf() === separateNumberRight.valueOf(), separateNumberRight.extra === undefined);
console.log('boxed zero sign', Object.is(new Number(-0).valueOf(), -0), Object.is(new Number(0).valueOf(), 0));

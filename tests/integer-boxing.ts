export {};
function observe(value: unknown): void {
    console.log(typeof value, value, Object.is(value, -0));
}
for (const value of [0, -0, 1, 255, 256, -1, 0.5, NaN, Infinity, -Infinity, 2147483647, 2147483648, 4294967295]) observe(value);
observe(0); observe(-0); observe(255); observe(256);
observe(-1 >>> 0); observe(-1 >> 0); observe(1 << 31); observe(~-1);
const big: any = 7n;
observe(big & big); observe(~big);
let count = 0;
function input(value: number): number { count++; return value; }
observe(input(511) & input(255));
console.log('calls', count);
const boxed: unknown[] = [];
for (let i = 0; i < 3; i++) boxed.push(i);
console.log(boxed.join(','), Object(1) === Object(1));
const map = new Map<unknown, string>();
map.set(0, 'zero'); map.set(255, 'last');
console.log(map.get(0 | 0), map.get(511 & 255), map.get(-0));
console.log(JSON.stringify({value: 511 & 255}), JSON.stringify({value: -0}));

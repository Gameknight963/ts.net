async function main(): Promise<void> {
    console.log('awaited', await Promise.resolve(1));
    console.log('multiple', await 2, await 3);
    console.log('hasOwn', Object.hasOwn({x: 4}, await 'x'));
    console.log('clock', typeof (performance.now as any)(await Promise.resolve()));
}
function shadow(console: {log(value: number): number}): number {return console.log(5);}
async function shadowAsync(console: {log(value: number): number}): Promise<number> {return console.log(await 6);}
console.log('shadow', shadow({log: value => value + 10}));
shadowAsync({log: value => value + 10}).then(value => console.log('async shadow', value));
main();

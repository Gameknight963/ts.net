import defaults, {values, started} from './async-generator-module.js';
export async function* local() { yield* values(20); }
async function main() {
 const source = values(10); console.log(started);
 console.log(JSON.stringify(await source.next()), started);
 console.log(JSON.stringify(await source.next()));
 console.log(JSON.stringify(await source.next()));
 const result: number[] = [];
 for await (const value of defaults()) result.push(value);
 for await (const value of local()) result.push(value);
 console.log(result.join(','), started);
}
main().catch(error => console.log('unexpected', error));

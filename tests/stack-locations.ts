import {importedError} from "./stack-location-helper";
const imported=importedError();
function normalized(error:Error){return (error.stack??"").replace(/:line (\d+)/g,":$1");}
console.log("module",normalized(imported).includes("stack-location-helper.ts:2"),normalized(imported).includes("stack-locations.ts:2"));
async function fail(){
    await Promise.resolve();
    throw new Error("async location");
}
fail().catch(error=>console.log("async",normalized(error).includes("stack-locations.ts:7")));
function* generate(){
    yield 1;
    return new Error("generator location");
}
const iterator=generate();iterator.next();console.log("generator",normalized(iterator.next().value as Error).includes("stack-locations.ts:12"));

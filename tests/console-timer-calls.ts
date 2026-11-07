console.time(); console.timeLog(undefined,"default-data"); console.timeEnd();
const start=console.time, end=console.timeEnd;
start("work");console.timeLog("work","payload",42);end("work");
async function later() {console.time("async");await Promise.resolve();console.timeLog("async","awaited");console.timeEnd("async");}
later();

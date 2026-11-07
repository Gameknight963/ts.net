debugger;
console.log('top level');
function calculate(): number {
    let result = 0;
    for (let index = 0; index < 4; index++) {debugger; result += index;}
    if (result === 6) debugger;
    mark: {debugger; break mark;}
    return result;
}
console.log('function', calculate());
try {debugger; throw 'failure';} catch(error) {debugger; console.log('caught', error);} finally {debugger; console.log('finally');}
const method = {run(): number {debugger; return 7;}};
console.log('method', method.run());
async function run(): Promise<void> {
    debugger;
    console.log('before');
    await 0;
    debugger;
    console.log('after');
    try {await 0; debugger;} finally {debugger; console.log('async finally');}
}
run();
console.log('synchronous end');

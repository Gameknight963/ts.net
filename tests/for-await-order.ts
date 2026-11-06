let tick=0;
function pump():void {tick++;if(tick<300)queueMicrotask(pump);}
queueMicrotask(pump);
async function run():Promise<void> {
    console.log('start',tick);
    for await(const value of [1,2])console.log('plain',value,tick);
    console.log('exhausted',tick);
    for await(const value of [] as number[])console.log('wrong',value);
    console.log('empty',tick);
    for await(const value of [Promise.resolve(3)]){console.log('break',value,tick);break;}
    console.log('after break',tick);
    try {for await(const value of [4]){console.log('throw',value,tick);throw 'body';}}
    catch(error){console.log('caught',error,tick);}
    for await(const value of [5,6]){try{console.log('continue',value,tick);continue;}finally{await 0;console.log('finally',tick);}}
    console.log('after continue',tick);
    try {for await(const value of [Promise.reject('next')])console.log('wrong',value);}
    catch(error){console.log('next rejected',error,tick);}
    console.log('end',tick);
}
run().then(()=>console.log('resolved',tick));

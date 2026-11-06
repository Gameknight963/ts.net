// Iterator-result promises must still assimilate inherited then properties.
async function run():Promise<void> {
    (Object.prototype as any).then=function(this:any,resolve:(value:any)=>void):void {
        (Object.prototype as any).then=undefined;
        console.log('intercept result',this.value,this.done);
        resolve({value:9,done:false});
    };
    for await(const value of [1]){console.log('replaced value',value);break;}
    try {
        for await(const value of [2]) {
            (Object.prototype as any).then=function():void{(Object.prototype as any).then=undefined;throw 'close error';};
            throw 'body error';
        }
    } catch(error){console.log('preserved throw',error);}
    try {
        for await(const value of [3]) {
            (Object.prototype as any).then=function():void{(Object.prototype as any).then=undefined;throw 'close error';};
            break;
        }
    } catch(error){console.log('break close error',error);}
    (Object.prototype as any).then=function(this:any,resolve:(value:any)=>void):void {
        (Object.prototype as any).then=undefined;
        resolve(4);
    };
    try {for await(const value of [4])console.log('wrong',value);}
    catch(error){console.log('invalid result',(error as Error).name);}
}
run().then(()=>console.log('done'));

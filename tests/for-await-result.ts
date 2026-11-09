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
async function constructorReads():Promise<void> {
    let reads=0;
    function result(value:number,done:boolean):Promise<IteratorResult<number>> {
        const promise=Promise.resolve<IteratorResult<number>>({value,done});
        Object.defineProperty(promise,'constructor',{get(){reads++;return Promise;},configurable:true});
        return promise;
    }
    function source():AsyncIterable<number> {
        return {[Symbol.asyncIterator](){let value=0;return {
            next(){return result(value++,value>2);},
            return(){return result(0,true);}
        };}};
    }
    for await(const value of source()){console.log('constructor break',value);break;}
    console.log('next and return constructor reads',reads);
    reads=0;
    for await(const value of source())console.log('constructor value',value);
    console.log('next constructor reads',reads);
    reads=0;
    const values=await (Array as any).fromAsync(source());
    console.log('fromAsync constructor reads',reads,values.join(','));
}
run().then(async()=>{console.log('done');await constructorReads();});

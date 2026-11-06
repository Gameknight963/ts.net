let saved:any;
function cleanup(label:string,fail=false):void {
    (Object.prototype as any).return=function(this:any):any {
        saved=this;
        const item=this.next();
        console.log('close',label,item.value,item.done);
        if(fail)throw 'close failure';
        return {done:true};
    };
}
function clear():void{(Object.prototype as any).return=undefined;}
cleanup('array');for(const value of [1,2,3]){console.log('first',value);break;}clear();
console.log('saved',saved.next().value,saved.next().done);const list=[...saved];console.log('sticky',list.length);
cleanup('string');for(const value of 'a\uD83D\uDE00b'){console.log('first string',value);break;}clear();
console.log('saved string',saved.next().value,saved.next().done);
function args(first:number,second:number):void {cleanup('arguments');for(const value of arguments)break;clear();}
args(4,5);
cleanup('pattern');const [head]=[6,7,8];console.log('head',head);clear();
cleanup('empty pattern');const []=[1,2];clear();
clear();
function install():number{cleanup('installed by default');return 9;}
const [defaulted=install()]=[undefined,10];console.log('defaulted',defaulted);clear();
try {const [value=(()=>{cleanup('throwing default',true);throw 'default failure';})()]=[undefined,11];console.log(value);}catch(error){console.log('default wins',error);}clear();
cleanup('exhausted');const [missing=12]=[] as number[];console.log('exhausted',missing);clear();
cleanup('mapper',true);try{Array.from([1,2],()=>{throw 'mapper failure';});}catch(error){console.log('mapper wins',error);}clear();
cleanup('map constructor',true);try{new Map([1,2] as any);}catch(error){console.log('constructor error',(error as Error).name);}clear();
class Broken extends Promise<any>{static override resolve(value?:any):any{throw 'resolve failure';}}
cleanup('promise',true);Broken.all([1,2]).catch(error=>console.log('promise error',error));clear();
function returning():number{cleanup('return');for(const value of [1,2])return value;return 0;}
console.log('return value',returning());clear();
cleanup('break error',true);try{for(const value of [1,2])break;}catch(error){console.log('close error',error);}clear();
cleanup('throw error',true);try{for(const value of [1,2])throw 'body failure';}catch(error){console.log('throw wins',error);}clear();
async function run():Promise<void> {
    cleanup('for await');for await(const value of [Promise.resolve(1),2,3])break;clear();
    console.log('async saved',saved.next().value,saved.next().done);
    cleanup('async ordinary');for(const value of [1,2]){await 0;break;}clear();
    cleanup('async pattern');const [value=await 2]=[undefined,3];console.log('async value',value);clear();
    cleanup('rejected value',true);try{for await(const value of [Promise.reject('value failure'),2])console.log(value);}catch(error){console.log('value wins',error);}clear();
    cleanup('async throw',true);try{for await(const value of [1,2])throw 'async body failure';}catch(error){console.log('async throw wins',error);}clear();
    cleanup('async break failure',true);try{for await(const value of [1,2])break;}catch(error){console.log('async close wins',error);}clear();
    console.log('done');
}
run();

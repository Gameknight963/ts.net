import {first,awaited} from './sequence-cleanup-values';
const common=Object.getPrototypeOf(Object.getPrototypeOf(new Map().keys()));
let tick=0;
function pump():void{tick++;if(tick<100)queueMicrotask(pump);}
queueMicrotask(pump);
let received:any;
const parent={
    get return():any {
        received=this;
        console.log('get return',typeof (this as any).next,tick);
        return function(this:any):any {
            console.log('call return',received===this,this.next().value,tick);
            return {
                get done():boolean{console.log('get done',tick);return true;},
                get value():Promise<number>{console.log('get value',tick);return Promise.resolve(8);}
            };
        };
    }
};
Object.setPrototypeOf(common,parent);
console.log('module first',first(),tick);
awaited().then(value=>console.log('module awaited',value,tick));

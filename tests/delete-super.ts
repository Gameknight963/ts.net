class Base {[key: string]: any; static [key: string]: any;}
(Base.prototype as any).value = 1;
(Base as any).value = 2;
const key = {toString(): string {console.log('key conversion');return 'value';}};
const badKey = {toString(): never {throw 'key failure';}};
function computed(): string {console.log('key expression');return 'value';}
class Derived extends Base {
    remove(): void {
        try {console.log(delete super.value);} catch(error) {console.log('named', (error as Error).name);}
        try {console.log(delete super[computed()]);} catch(error) {console.log('computed', (error as Error).name);}
        try {console.log(delete super[key as any]);} catch(error) {console.log('converted', (error as Error).name);}
        try {console.log(delete super[badKey as any]);} catch(error) {console.log('no conversion', (error as Error).name);}
        try {console.log(delete super[(() => {throw 'expression failure';})()]);} catch(error) {console.log('expression wins', error);}
    }
    static remove(): void {
        try {console.log(delete super.value);} catch(error) {console.log('static', (error as Error).name);}
    }
    async removeAsync(): Promise<void> {
        try {console.log(delete super[await Promise.resolve(key) as any]);} catch(error) {console.log('awaited', (error as Error).name);}
        try {console.log(delete super[await Promise.reject('await failure')]);} catch(error) {console.log('rejection', error);}
    }
}
const instance = new Derived(); instance.remove(); Derived.remove();
console.log('unchanged', (Base.prototype as any).value, (Base as any).value);
instance.removeAsync();
class BeforeSuper extends Base {
    constructor() {
        const remove = () => delete super[computed()];
        try {console.log(remove());} catch(error) {console.log('before super', (error as Error).name);}
        super();
    }
}
new BeforeSuper();

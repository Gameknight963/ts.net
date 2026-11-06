function shrink(): string {
    const values = [1, 2];
    const key: any = {toString: () => { values.length = 0; return "key"; }};
    let result = "";
    for(let i=0;i<2;i++) {
        const object = {[key]: 1};
        result += typeof values[i];
    }
    return result;
}
function grow(): number {
    const values = [1, 2];
    const key: any = {toString: () => { values.push(3); values[0] = 9; return "key"; }};
    let result = 0;
    for(let i=0;i<2;i++) {
        const object = {[key]: 1};
        result += values[i];
    }
    return result;
}
function binding(): string {
    const values: any[] = [1, "two"];
    const key: any = {toString: () => { values.length = 0; return "key"; }};
    let result = "";
    for(let i=0;i<2;i++) {
        const {[key]: value} = ({key: 1} as any);
        result += typeof values[i];
    }
    return result;
}
function arrayKey(): string {
    const values = [1, 2];
    const key: any = [{toString: () => { values.length = 0; return "key"; }}];
    let result = "";
    for(let i=0;i<2;i++) {
        const object = {[key]: 1};
        result += typeof values[i];
    }
    return result;
}
console.log(shrink(), grow(), binding(), arrayKey());

function nullishKeyOrder(receiver:any):void {
    const key:any={toString():string {console.log('unexpected key conversion');return 'x';}};
    try { console.log(receiver[key]); } catch(error) { console.log('nullish read',error instanceof TypeError); }
    try { receiver[key]=1; } catch(error) { console.log('nullish write',error instanceof TypeError); }
}
nullishKeyOrder(null);
nullishKeyOrder(undefined);

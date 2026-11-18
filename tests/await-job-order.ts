export {};
const order: string[] = [];
async function resume(value: any): Promise<any> {
    order.push('enter');
    const result = await value;
    order.push('resume:' + result);
    return result;
}
async function tail(value: any): Promise<any> {return await value;}
const first = resume(1).then(value => {order.push('first:' + value); return value;});
let finish!: (value: number) => void;
const pending = new Promise<number>(resolve => {finish = resolve;});
const second = resume(pending).then(value => {order.push('second:' + value); return value;});
const thenable = {then(resolve: (value: number) => void, reject: (value: any) => void){
    order.push('thenable'); resolve(3); reject('ignored'); throw 'also ignored';
}};
const third = resume(thenable).then(value => {order.push('third:' + value); return value;});
queueMicrotask(() => {order.push('finish'); finish(2);});
queueMicrotask(() => order.push('marker'));
const reason = {message: 'reason'};
const rejected = tail(Promise.reject(reason)).catch(error => {order.push('rejected:' + (error === reason)); return 4;});
const throwing = resume(5).then(() => {throw reason;}).catch(error => {order.push('thrown:' + (error === reason)); return 5;});
async function regions(): Promise<number> {
    let total = await 1;
    try {await Promise.reject(2);} catch (error) {total += error as number;}
    finally {total += await 3;}
    total += await 4;
    return total;
}
let thenReads = 0;
const intrinsic = Promise.resolve(6);
Object.defineProperty(intrinsic, 'then', {get(){thenReads++; throw 'public then';}, configurable: true});
const bypass = tail(intrinsic).then(value => {order.push('bypass:' + value); return value;});
async function* generator(): AsyncGenerator<number> {yield await 7; yield await Promise.resolve(8);}
async function consume(): Promise<number> {let total = 0; for await (const value of generator()) total += value; return total;}
Promise.all([first, second, third, rejected, throwing, regions(), bypass, consume()]).then(values => {
    console.log('values', values.join(','), thenReads);
    console.log('order', order.join('|'));
});

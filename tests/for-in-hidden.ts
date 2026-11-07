const root: any = Object.prototype;
const promise = Promise.resolve(0);
root.push = 'inherited push';
root.then = 'inherited then';
root.sin = 'inherited sin';
function inheritedKeys(value: any): string {
    const result: string[] = [];
    for (const key in value) result.push(key);
    return result.join(',');
}
console.log('array', inheritedKeys(['item']));
console.log('math', inheritedKeys(Math));
console.log('promise', inheritedKeys(promise));
delete root.push;
delete root.then;
delete root.sin;

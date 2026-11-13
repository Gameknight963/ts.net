const deletionKey = 'unused';
const deletionTarget: {unused?: number} = {unused: 1};
console.log('delete', delete deletionTarget[deletionKey], Object.hasOwn(deletionTarget, 'unused'));
function work(): number {
    let sum = 0;
    for (let index = 0; index < 100000; index++) {
        const value = {first: index, second: index + 1};
        sum += value.first + value.second;
    }
    return sum;
}
console.log(work());

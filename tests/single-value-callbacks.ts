const callbackValues: (number | string | undefined)[] = [1, 'a', undefined, 2, 'bb'];
console.log('mixed', callbackValues.map(value => typeof value === 'number' ? value + 1 : typeof value === 'string' ? value.length : -1).join(','));
console.log('zero', callbackValues.map(() => 'constant').join(','));
console.log('objects', callbackValues.map(value => ({value})).map(value => String(value.value)).join(','));
let callbackVisits = 0;
const callbackSource: (number | string)[] = [1, 'a', 2];
console.log('mutation', callbackSource.map(value => { if (++callbackVisits === 1) callbackSource[1] = 'changed'; return String(value); }).join(','), callbackVisits);
const callbackContext = {prefix: 'context:'};
console.log('this', callbackValues.map(function(this: typeof callbackContext, value) { return this.prefix + String(value); }, callbackContext).join(','));
console.log('arguments', callbackValues.map(function(value) { return arguments.length + ':' + (arguments[0] === value) + ':' + (arguments[2] === callbackValues); }).join(','));
console.log('rest', callbackValues.map((...values: any[]) => values.length + ':' + (values[2] === callbackValues)).join(','));
console.log('default', callbackValues.map((value = 'missing') => String(value)).join(','));
function makeCallback(): (value: number | string | undefined) => string {
    let visits = 0;
    return value => ++visits + ':' + String(value);
}
const capturedCallback = makeCallback();
console.log('capture', callbackValues.map(capturedCallback).join(','), capturedCallback(9));
Promise.resolve('promise').then(value => value + ':value').then(value => console.log('promise-value', value));
Promise.resolve('ignored').then(() => 'zero').then(value => console.log('promise-zero', value));
Promise.resolve<string | undefined>(undefined).then((value = 'default') => value).then(value => console.log('promise-default', value));
Promise.resolve(3).then(function(value) { return arguments.length + ':' + arguments[0] + ':' + value; }).then(value => console.log('promise-arguments', value));
Promise.resolve(4).then((...values) => values.length + ':' + values[0]).then(value => console.log('promise-rest', value));
Promise.resolve(5).then(String).then(value => console.log('promise-native', value));


function mappedCapture(values: (number | string | undefined)[]): string {
    let visits = 0;
    const callback = (value: number | string | undefined): string => ++visits + ':' + String(value);
    return values['map'](callback).join(',') + ':' + visits;
}
console.log('mapped-capture', mappedCapture(callbackValues));
let reassignedCallback = (value: number | string | undefined): string => 'first:' + String(value);
reassignedCallback = value => 'second:' + String(value);
console.log('reassigned', callbackValues.map(reassignedCallback).join(','));

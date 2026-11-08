export {};
[Number.isNaN] = [(value: unknown) => value === 'array'];
console.log('array target', Number.isNaN('array'), Number.isNaN(NaN));
[Object.keys] = [() => ['array']];
console.log('array keys', Object.keys({a: 1}).join(','));

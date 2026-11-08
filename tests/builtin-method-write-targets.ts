export {};
({predicate: Number.isNaN} = {predicate: (value: unknown) => value === 'object'});
console.log('object target', Number.isNaN('object'), Number.isNaN(NaN));
({keys: Object.keys} = {keys: () => ['object']});
console.log('keys target', Object.keys({a: 1}).join(','));

export {};
for (Number.isNaN of [(value: unknown) => value === 'loop']) console.log('loop target', Number.isNaN('loop'), Number.isNaN(NaN));
for (Object.keys of [() => ['loop']]) console.log('loop keys', Object.keys({a: 1}).join(','));

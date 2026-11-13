const originalReflectGet = Reflect.get;
Reflect.get = Reflect.deleteProperty as any;
try {
    const target = {first: true, second: 2};
    console.log('computed-replacement', Reflect['get'](target, 'first'), Object.hasOwn(target, 'first'), Object.keys(target).join(','));
} finally { Reflect.get = originalReflectGet; }

const originalReflectHas = Reflect.has;
Reflect.has = Reflect.deleteProperty;
try {
    const target = {hasField: 1, retained: 2};
    console.log('compatible-replacement', Reflect['has'](target, 'hasField'), Object.hasOwn(target, 'hasField'), Object.keys(target).join(','));
} finally { Reflect.has = originalReflectHas; }

const originalReflectGet = Reflect.get;
Reflect.get = Reflect.deleteProperty as any;
try {
    const target = {first: true, second: 2};
    console.log('computed-replacement', Reflect['get'](target, 'first'), Object.hasOwn(target, 'first'), Object.keys(target).join(','));
} finally { Reflect.get = originalReflectGet; }

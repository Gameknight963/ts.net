export {};
function inspect(first: number, second: number) {
    const args: any = arguments;
    args.extra = 9;
    Object.defineProperty(args, '0', {get() {return 7;}, configurable: true, enumerable: false});
    Object.defineProperty(args, 'length', {value: 1, writable: false});
    console.log('values', args[0], first, args[1], args.length, [...args].join(','));
    console.log('keys', Object.keys(args).join(','), Object.getOwnPropertyNames(args).join(','));
    try {args.length = 2;} catch (error) {console.log('readonly length', (error as Error).name);}
    console.log('delete', Reflect.deleteProperty(args, '1'), Object.hasOwn(args, '1'), args[1]);
    Object.freeze(args);
    console.log('frozen', Object.isFrozen(args), Reflect.defineProperty(args, 'extra', {value: 10}), Reflect.deleteProperty(args, 'callee'));
}
inspect(1, 2);
function deleted(first: number, second: number) {
    const args: any = arguments;
    console.log('delete index', Reflect.deleteProperty(args, '0'), args[0], first, args.length);
    args[0] = 8;
    console.log('recreate', args[0], first, Object.keys(args).join(','));
    console.log('delete length', Reflect.deleteProperty(args, 'length'), Object.hasOwn(args, 'length'), args.length, [...args].length);
}
deleted(3, 4);
function locked(first: number) {
    const args: any = arguments;
    Object.freeze(args);
    console.log('direct freeze', Object.isFrozen(args), Object.getOwnPropertyDescriptor(args, '0')!.writable);
    try {args[0] = 99;} catch (error) {console.log('readonly index', (error as Error).name);}
    console.log('unchanged', args[0], first);
}
locked(5);
function closed(first: number) {
    const args: any = arguments;
    Object.preventExtensions(args);
    Object.defineProperty(args, '0', {value: 6, writable: false});
    console.log('closed', args[0], first, Object.isExtensible(args), Reflect.defineProperty(args, 'newKey', {value: 1}));
    const callee = Object.getOwnPropertyDescriptor(args, 'callee')!;
    console.log('callee', callee.enumerable, callee.configurable, Reflect.defineProperty(args, 'callee', {get: callee.get}));
    try {args.callee;} catch (error) {console.log('callee read', (error as Error).name);}
}
closed(1);

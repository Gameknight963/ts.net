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
function inherited(first: number, second: number) {
    const args: any = arguments;
    const prototype = {get 0() {return 20;}, get length() {return 2;}};
    Object.setPrototypeOf(args, prototype);
    Reflect.deleteProperty(args, '0');
    Reflect.deleteProperty(args, 'length');
    console.log('inherited values', args[0], args.length, first, Object.hasOwn(args, '0'));
    const collected = Array.from(args);
    console.log('inherited iteration', collected.join(','));
    Object.defineProperty(args, '0', {value: 21, enumerable: true});
    Object.defineProperty(args, '9', {value: 29, enumerable: true});
    console.log('ordinary indices', args.length, Object.keys(args).join(','), args[9]);
}
inherited(1, 2);
function changingLength(first: number, second: number) {
    const args: any = arguments;
    let reads = 0;
    Object.defineProperty(args, 'length', {get() {reads++; return reads < 3 ? 2 : 0;}});
    console.log('live length', [...args].join(','), reads);
}
changingLength(3, 4);

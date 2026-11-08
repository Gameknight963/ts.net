export {};
for (const method of ['map', 'filter', 'slice', 'concat', 'splice', 'flat', 'flatMap']) {
    for (const kind of ['setter', 'readonly', 'sealed', 'frozen', 'nonextensible']) {
        let calls = 0;
        const prototype = Object.create(Array.prototype);
        if (kind === 'setter') Object.defineProperty(prototype, '0', {set(value) {calls++;}, configurable: true});
        else if (kind === 'readonly') Object.defineProperty(prototype, '0', {value: 99, writable: false, configurable: true});
        const destination: any[] = kind === 'sealed' || kind === 'frozen' || kind === 'nonextensible' ? [9, 8] : [];
        if (kind === 'sealed') Object.seal(destination);
        if (kind === 'frozen') Object.freeze(destination);
        if (kind === 'nonextensible') Object.preventExtensions(destination);
        if (kind === 'setter' || kind === 'readonly') Object.setPrototypeOf(destination, prototype);
        function Result(this: any, length: number): any {return destination;}
        const source: any = [1, 2];
        source.constructor = {[Symbol.species]: Result};
        try {
            let result: any;
            if (method === 'map') result = source.map((value: number) => value * 2);
            else if (method === 'filter') result = source.filter(() => true);
            else if (method === 'splice') result = source.splice(0, 2);
            else if (method === 'flatMap') result = source.flatMap((value: number) => [value]);
            else result = source[method]();
            console.log(method, kind, result === destination, calls, Object.hasOwn(result, '0'), result[0], result.length);
        } catch (error) {console.log('error', method, kind, (error as Error).name, calls);}
    }
}

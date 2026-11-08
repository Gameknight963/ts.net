export {};
async function work() {
    for (const method of ['from', 'of', 'fromAsync']) {
        for (const kind of ['setter', 'readonly', 'sealed', 'frozen', 'nonextensible']) {
            let calls = 0, closed = 0;
            const destination: any[] = kind === 'setter' || kind === 'readonly' ? [] : [9, 8];
            if (kind === 'setter' || kind === 'readonly') {
                const prototype = Object.create(Array.prototype);
                Object.defineProperty(prototype, '0', kind === 'setter' ? {set() {calls++;}, configurable: true} : {value: 99, writable: false, configurable: true});
                Object.setPrototypeOf(destination, prototype);
            }
            if (kind === 'sealed') Object.seal(destination);
            if (kind === 'frozen') Object.freeze(destination);
            if (kind === 'nonextensible') Object.preventExtensions(destination);
            function Result(this: any): any {return destination;}
            const input = {[Symbol.iterator]() {
                let index = 0;
                return {next() {return index < 2 ? {done: false, value: ++index} : {done: true};}, return() {closed++; return {};}};
            }};
            try {
                let result: any;
                if (method === 'of') result = (Array.of as Function).call(Result, 1, 2);
                else if (method === 'from') result = (Array.from as Function).call(Result, input);
                else result = await (Array as any).fromAsync.call(Result, input);
                console.log(method, kind, result === destination, calls, closed, Object.hasOwn(result, '0'), result[0], result.length);
            } catch (error) {console.log('error', method, kind, (error as Error).name, calls, closed);}
        }
    }
}
work();

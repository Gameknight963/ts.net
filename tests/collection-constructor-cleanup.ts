export {};
for (const family of [Map, WeakMap]) {
    for (const failure of ['primitive', 'key', 'value', 'adder']) {
        for (const cleanup of ['normal', 'throws', 'primitive']) {
            let effects = '';
            const key = {};
            const entry: any = failure === 'primitive' ? 1 : {
                get 0() {effects += 'K'; if (failure === 'key') throw new Error('key'); return key;},
                get 1() {effects += 'V'; if (failure === 'value') throw new Error('value'); return 42;}
            };
            const source: any = {
                [Symbol.iterator]() {effects += 'I'; return this;},
                next() {effects += 'N'; return {done: false, value: entry};},
                return() {effects += 'R'; if (cleanup === 'throws') throw new Error('cleanup'); return cleanup === 'primitive' ? 1 : {};}
            };
            class Collection extends (family as any) {
                constructor(values: any) {super(values);}
                set(key: unknown, value: unknown): this {effects += 'A'; throw new Error('adder');}
            }
            try {new Collection(source);}
            catch (error) {console.log(family.name, failure, cleanup, effects, failure === 'primitive' ? (error as Error).name : (error as Error).message);}
        }
    }
}
let calls = 0;
class Captured extends Map<number, number> {
    set(key: number, value: number): this {
        calls++;
        if (calls === 1) this.set = function () {throw new Error('must use captured adder');};
        return super.set(key, value);
    }
}
const map = new Captured([[1, 2], [3, 4]]);
console.log('captured', calls, map.size, map.get(3));
for (const family of [Set, WeakSet]) {
    let effects = '';
    const source: any = {
        [Symbol.iterator]() {return this;},
        next() {return {done: false, value: {}};},
        return() {effects += 'R'; throw new Error('cleanup');}
    };
    class Collection extends (family as any) {
        constructor(values: any) {super(values);}
        add(value: unknown): this {effects += 'A'; throw new Error('adder');}
    }
    try {new Collection(source);}
    catch (error) {console.log('set cleanup', family.name, effects, (error as Error).message);}
}
for (const family of [Map, Set, WeakMap, WeakSet]) {
    for (const failure of ['next', 'done', 'value']) {
        let closes = 0;
        const source: any = {
            [Symbol.iterator]() {return this;},
            next() {
                if (failure === 'next') throw new Error('next');
                return {
                    get done() {if (failure === 'done') throw new Error('done'); return false;},
                    get value() {throw new Error('value');}
                };
            },
            return() {closes++; return {};}
        };
        try {new (family as any)(source);}
        catch (error) {console.log('step failure', family.name, failure, closes, (error as Error).message);}
    }
}

export {};
for (const mode of ['all', 'allSettled', 'race', 'any']) {
    for (const failure of ['resolve getter', 'resolve call', 'then getter', 'then call', 'next', 'done', 'value']) {
        let effects = '';
        class Recorded {
            constructor(executor: any) {
                effects += 'C';
                executor(() => console.log('unexpected resolve'), (error: Error) => console.log('rejected', mode, failure, effects, error.message));
            }
            static get resolve(): any {
                effects += 'R';
                if (failure === 'resolve getter') throw new Error(failure);
                return function (this: unknown) {
                    effects += this === Recorded ? 'r' : '!';
                    if (failure === 'resolve call') throw new Error(failure);
                    return {get then() {
                        effects += 'T';
                        if (failure === 'then getter') throw new Error(failure);
                        return function () {effects += 't'; throw new Error(failure);};
                    }};
                };
            }
        }
        const source: any = {
            [Symbol.iterator]() {effects += 'I'; return this;},
            next() {
                effects += 'N';
                if (failure === 'next') throw new Error(failure);
                return {
                    get done() {effects += 'D'; if (failure === 'done') throw new Error(failure); return false;},
                    get value() {effects += 'V'; if (failure === 'value') throw new Error(failure); return 1;}
                };
            },
            return() {effects += 'X'; throw new Error('cleanup must not replace original error');}
        };
        console.log('result', (Promise as any)[mode].call(Recorded, source) instanceof Recorded);
    }
}

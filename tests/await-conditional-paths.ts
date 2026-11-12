export {};

async function occasional(flag: boolean): Promise<number> {
    let total = 0;
    for (let i = 0; i < 20; i++) {
        if (flag && i === 17) await 0;
        else total += 2;
        total += i;
    }
    return total;
}

async function opposite(): Promise<number> {
    let total = 0;
    for (let i = 0; i < 8; i++) {
        if (i < 6) total += i;
        else await 0;
        total += 3;
    }
    return total;
}

async function awaitedCondition(): Promise<number> {
    let total = 0;
    for (let i = 0; i < 5; i++) {
        if (await Promise.resolve(i === 3)) await 0;
        else total++;
        total += i;
    }
    return total;
}

async function scopedTail(): Promise<string> {
    const callbacks: (() => number)[] = [];
    for (let i = 0; i < 4; i++) {
        if (i === 2) await 0;
        const value = i * 3;
        callbacks.push(() => value);
    }
    return callbacks.map(callback => callback()).join(',');
}

async function throwing(flag: boolean): Promise<string> {
    let result = '';
    try {
        if (flag) await Promise.reject('rejected');
        else result += 'sync';
        result += ':tail';
    } catch (error) {
        result += String(error);
    } finally {
        result += ':finally';
    }
    return result;
}

Promise.all([occasional(false), occasional(true), opposite(), awaitedCondition(), scopedTail(), throwing(false), throwing(true)])
    .then(values => console.log('conditional await paths', values.join('|')));

function* occasionalYield(): Generator<number, number, unknown> {
    let total = 0;
    for (let i = 0; i < 4; i++) {
        if (i === 2) yield i;
        else total += i;
        total += 10;
    }
    return total;
}
const generator = occasionalYield();
console.log('conditional yield', JSON.stringify(generator.next()), JSON.stringify(generator.next()));

async function loopTransfers(): Promise<number> {
    let total = 0;
    for (let i = 0; i < 8; i++) {
        if (i === 3) await 0;
        else if (i === 1) continue;
        else if (i === 6) break;
        total += i;
    }
    return total;
}
loopTransfers().then(value => console.log('conditional loop transfers', value));

async function receiverAndArguments(this: {tag: number}, flag: boolean, extra: number): Promise<number> {
    if (flag) await 0;
    return this.tag + arguments.length + extra;
}
Promise.all([
    receiverAndArguments.call({tag: 10}, false, 3),
    receiverAndArguments.call({tag: 20}, true, 4)
]).then(values => console.log('conditional receiver and arguments', values.join(',')));

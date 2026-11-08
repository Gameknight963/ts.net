export {};
async function main(): Promise<void> {
    for (const Constructor of [CountQueuingStrategy,ByteLengthQueuingStrategy]) {
        const value = new Constructor({highWaterMark: 4});
        const second = new Constructor({highWaterMark: 5});
        console.log('metadata', Constructor.name, Constructor.length, value.highWaterMark, value.size.name, value.size.length);
        console.log('identity', value.size === second.size, Object.keys(value).length, Object.prototype.toString.call(value));
        console.log('prototype', Object.getPrototypeOf(value) === Constructor.prototype, value.constructor === Constructor);
        Object.freeze(value);console.log('frozen', value.highWaterMark, Object.isFrozen(value));
        for (const mark of [NaN,Infinity,-Infinity,-1,0,-0,null,'3'] as any[]) {
            const strategy = new Constructor({highWaterMark: mark});
            console.log('mark', strategy.highWaterMark, Object.is(strategy.highWaterMark,-0));
        }
        for (const options of [undefined,null,3,{}, {highWaterMark: undefined}, {highWaterMark: Symbol('bad')}] as any[]) {
            try {new Constructor(options);} catch (error) {console.log('invalid', (error as Error).name);}
        }
        try {(Constructor as any)({highWaterMark: 1});} catch (error) {console.log('requires new', (error as Error).name);}
        for (const name of ['highWaterMark','size']) {
            const descriptor = Object.getOwnPropertyDescriptor(Constructor.prototype,name)!;
            console.log('descriptor', name, descriptor.enumerable, descriptor.configurable, descriptor.set === undefined);
            try {descriptor.get!.call({});} catch (error) {console.log('brand', (error as Error).name);}
        }
    }
    const count = new CountQueuingStrategy({highWaterMark: 2});
    const countSize = count.size as any;
    console.log('count sizes', countSize(), countSize.call(null), countSize(Symbol('item')));
    const bytes = new ByteLengthQueuingStrategy({highWaterMark: 10});
    const byteSize = bytes.size as any;
    console.log('byte sizes', byteSize(new Uint8Array(3)), byteSize(new ArrayBuffer(4)), byteSize(new DataView(new ArrayBuffer(5))));
    let gets = 0;
    console.log('raw size', byteSize({get byteLength() {gets++;return 'raw';}}), gets);
    console.log('missing size', byteSize({}));
    for (const input of [null,undefined]) {
        try {byteSize(input);} catch (error) {console.log('invalid chunk', (error as Error).name);}
    }
    let coercions = '';
    const coerced = new CountQueuingStrategy({get highWaterMark() {coercions += 'get|';return {valueOf() {coercions += 'number|';return 6;}} as any;}});
    console.log('coercion', coerced.highWaterMark, coercions.startsWith('get|'), coercions.replaceAll('get|',''));
    class Child extends CountQueuingStrategy {field = 7;}
    const child = new Child({highWaterMark: 8});
    console.log('subclass', child instanceof Child, child instanceof CountQueuingStrategy, child.highWaterMark, child.field, child.size(undefined));

    let countController!: ReadableStreamDefaultController<string>;
    const countStream = new ReadableStream<string>({start(controller) {countController = controller;controller.enqueue('large chunk');}}, count);
    console.log('count queue', countController.desiredSize);
    const countReader = countStream.getReader();await countReader.read();
    console.log('count drained', countController.desiredSize);await countReader.cancel();
    let bytesController!: ReadableStreamDefaultController<Uint8Array>;
    const byteStream = new ReadableStream<Uint8Array>({start(controller) {bytesController = controller;controller.enqueue(new Uint8Array(3));controller.enqueue(new Uint8Array(2));}}, bytes);
    console.log('byte queue', bytesController.desiredSize);
    const byteReader = byteStream.getReader();await byteReader.read();
    console.log('byte drained', bytesController.desiredSize);await byteReader.cancel();
    for (const mark of [-1,NaN]) {
        const strategy = new CountQueuingStrategy({highWaterMark: mark});
        try {new ReadableStream({},strategy);} catch (error) {console.log('stream validates', (error as Error).name);}
    }
}
main().catch(error => console.log('failure', String(error)));

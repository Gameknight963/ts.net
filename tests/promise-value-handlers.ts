export {};
async function main() {
    const number = (value: number) => value + 1;
    const text = (value: string) => value.length;
    const object = (value: { count: number }) => ({ count: value.count + 1 });
    console.log("number", await Promise.resolve(7).then(number));
    console.log("text", await Promise.resolve("hello").then(text));
    console.log("object", (await Promise.resolve({ count: 7 }).then(object)).count);
    console.log("missing", await Promise.resolve().then(value => value === undefined));
    console.log("default", await Promise.resolve<number | undefined>(undefined).then((value = 9) => value + 1));
    console.log("arguments", await Promise.resolve(7).then(function(value) { return value + arguments.length; }));
    console.log("rest", await Promise.resolve(7).then((...values) => values.length + values[0]));
    console.log("receiver", await Promise.resolve(7).then(function(this: any, value) { return this === undefined ? value : -1; }));
    console.log("catch", await Promise.reject("hello").catch(text));
    console.log("reject branch", await Promise.reject(7).then(number, number));
    console.log("throw", await Promise.resolve(7).then(value => { throw value + 1; }).catch(number));
    let handler = number;
    const pending = Promise.resolve(7).then(handler);
    handler = value => value + 10;
    console.log("alias", await pending, await Promise.resolve(7).then(handler));
    let state = 1;
    const log: string[] = [];
    const result = Promise.resolve(7).then(value => { log.push("handler"); return value + state; });
    log.push("sync"); state = 10;
    console.log("order", await result, log.join(","));
    const separate = (value: number, extra?: number) => value + (extra ?? 0);
    console.log("extra", await Promise.resolve(7).then(separate));
}
main();

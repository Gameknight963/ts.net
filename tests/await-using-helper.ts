/// <reference lib="esnext.disposable" />
export let closed = 0;
export class Resource implements AsyncDisposable {
    closed = false;
    async [Symbol.asyncDispose]() {
        console.log("module cleanup start");
        await Promise.resolve();
        this.closed = true;
        closed++;
        console.log("module cleanup end");
    }
}
export async function run() {
    await using value = new Resource();
    console.log("module body", value.closed);
    return () => value.closed;
}

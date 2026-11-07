/// <reference lib="esnext.disposable" />
export let closed = 0;
export function create() {
    const stack = new DisposableStack();
    stack.defer(() => {closed++; console.log("module sync cleanup");});
    return stack;
}
export class AsyncStack extends AsyncDisposableStack {
    name = "module async";
    constructor() {
        super();
        this.defer(async () => {await Promise.resolve(); closed++; console.log(this.name);});
    }
}

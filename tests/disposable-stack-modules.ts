import {closed, create, AsyncStack} from "./disposable-stack-helper.js";
{using stack = create(); console.log("module body", closed, stack.disposed);}
console.log("after sync", closed);
async function run() {
    await using stack = new AsyncStack();
    console.log("async body", stack.disposed, stack instanceof AsyncDisposableStack);
}
run().then(() => console.log("after async", closed));

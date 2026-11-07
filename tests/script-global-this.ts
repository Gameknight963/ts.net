// @ts-expect-error Deliberately exercise the runtime of a script-global this capture.
console.log("script",this===globalThis,(()=>this)()===globalThis);
function plain(this:unknown){return this;}
console.log("strict-function",plain()===undefined);
// @ts-expect-error Deliberately exercise the runtime of a script-global this capture.
const object={ method(){return this===object;}, arrow:()=>this===globalThis };
console.log("receivers",object.method(),object.arrow());

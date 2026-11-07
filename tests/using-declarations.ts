/// <reference lib="esnext.disposable" />
function resource(name: string, failure?: string) {
    console.log("open", name);
    return {[Symbol.dispose]() {console.log("close", name); if (failure) throw failure;}};
}
{using first = resource("first"), second = resource("second"); console.log("body");}
function early(): number {using value = resource("return"); return 7;}
console.log("return", early());
for (let index = 0; index < 3; index++) {using value = resource("loop" + index); if (index === 0) continue; break;}
try {using value = resource("throw"); throw "body failure";} catch (error) {console.log("caught", error);}
try {using first = resource("bad1", "one"), second = resource("bad2", "two"); throw "body";} catch (error) {
    const value = error as SuppressedError;
    console.log("suppressed", value.name, value.error, value.suppressed.name, value.suppressed.error, value.suppressed.suppressed, error instanceof SuppressedError);
}
{using nothing = null; using absent = undefined; console.log("empty");}
const captured: any = resource("captured");
{using held = captured; captured[Symbol.dispose] = () => console.log("wrong method");}
try {using first = resource("before invalid"); using invalid = {} as any;} catch (error) {console.log("invalid", (error as Error).name);}
async function delayed(): Promise<number> {using value = resource("async body"); await 0; console.log("resumed"); return 9;}
delayed().then(value => console.log("async result", value));
let lookups = 0;
const getterResource = {get [Symbol.dispose](): () => void {lookups++; return function(this: any) {console.log("getter cleanup", this === getterResource, arguments.length);};}};
{using held = getterResource; console.log("lookup", lookups);}
function nested(): number {try {using held = resource("nested"); return 11;} finally {console.log("outer finally");}}
console.log("nested return", nested());
async function failing(): Promise<void> {using held = resource("async failure", "dispose"); await 0; throw "async body";}
failing().catch(error => console.log("async suppressed", error.name, error.error, error.suppressed));
using topLevel = resource("top");
console.log("end");
function captureDisposed() {
    using value = {closed: false, [Symbol.dispose]() {this.closed = true;}};
    function read(): boolean {return value.closed;}
    return read;
}
console.log("captured disposed", captureDisposed()());
class Startup {static {using value = resource("static"); console.log("static body");}}
switch (1) {case 1: {using held = resource("case block"); console.log("case body"); break;}}
try {using held = resource("single failure", "single");} catch (error) {console.log("single disposal error", error);}
try {using first = resource("normal bad1", "one"), second = resource("normal bad2", "two");} catch (error) {const value = error as SuppressedError; console.log("disposal errors", value.error, value.suppressed);}

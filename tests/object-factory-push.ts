export {};
function create(value: number) {return {value, step(amount: number): void {this.value += amount;}};}
function captured(value: number) {return {value, read(): number {return value;}};}
function ordinary(): void {
    const values: ReturnType<typeof create>[] = [];
    for (let i = 0; i < 12; i++) values.push(create(i));
    console.log('ordinary', values.length, values.map(value => value.value).join(','));
    const closures: ReturnType<typeof captured>[] = [];
    for (let i = 0; i < 5; i++) closures.push(captured(i));
    console.log('captured', closures.map(value => value.read()).join(','));
}
ordinary();

const original = Array.prototype.push;
let calls = 0;
function changed(this: unknown[], value: unknown): number {calls++; return original.call(this, value);}
function argument(value: number): number {Array.prototype.push = changed; return value;}
function withArguments(): void {
    const values: ReturnType<typeof create>[] = [];
    for (let i = 0; i < 3; i++) values.push(create(argument(i)));
    Array.prototype.push = original;
    console.log('argument changes push', calls, values.length);
}
withArguments();

calls = 0;
function effectful(value: number) {Array.prototype.push = changed; return {value};}
function withBody(): void {
    const values: {value: number}[] = [];
    for (let i = 0; i < 3; i++) values.push(effectful(i));
    Array.prototype.push = original;
    console.log('factory changes push', calls, values.length);
}
withBody();

function customPush(): void {
    const values: ReturnType<typeof create>[] = [];
    let gets = 0;
    Object.defineProperty(values, 'push', {get() {gets++; return original;}});
    for (let i = 0; i < 3; i++) values.push(create(i));
    console.log('getter', gets, values.length);
}
customPush();

function empty(): void {
    const values: ReturnType<typeof create>[] = [];
    Object.defineProperty(values, 'push', {get() {throw 'unexpected lookup';}});
    for (let i = 0; i < 0; i++) values.push(create(i));
    console.log('empty', values.length);
}
empty();

let gets = 0;
const source = {get value() {gets++; Array.prototype.push = changed; return 7;}};
function accessor(value: number) {return {value: value + source.value};}
function withAccessor(): void {
    calls = 0;
    const values: {value: number}[] = [];
    for (let i = 0; i < 3; i++) values.push(accessor(i));
    Array.prototype.push = original;
    console.log('initializer getter', gets, calls, values.map(value => value.value).join(','));
}
withAccessor();

function defaultValue(): number {Array.prototype.push = changed; return 9;}
function defaults(value = defaultValue()) {return {value};}
function withDefaults(): void {
    calls = 0;
    const values: {value: number}[] = [];
    for (let i = 0; i < 3; i++) values.push(defaults());
    Array.prototype.push = original;
    console.log('default argument', calls, values.map(value => value.value).join(','));
}
withDefaults();

let mutable = (value: number) => ({value});
mutable = function replacement(value: number) {Array.prototype.push = changed; return {value: -value};};
function withReplacement(): void {
    calls = 0;
    const values: {value: number}[] = [];
    for (let i = 0; i < 3; i++) values.push(mutable(i));
    Array.prototype.push = original;
    console.log('replaced factory', calls, values.map(value => value.value).join(','));
}
withReplacement();

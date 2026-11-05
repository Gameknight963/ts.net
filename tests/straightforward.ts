type Counter = { value: number };
function next(counter: Counter) { return ++counter.value; }
function defaults(counter: Counter, first = next(counter), second = first + 10) {
    return `${first}:${second}:${counter.value}`;
}
function optional(value?: number) { return value ?? 13; }
function rawDefault(value: any = 7) { return value; }
function stringOptional(value?: string) { return value ?? "fallback"; }
function boolOptional(value?: boolean) { return value ?? true; }
function refDefault(value: Counter = { value: 5 }) { return value; }
function mutateDefaults(value = 2, label = "x") {
    console.log(value++, ++value, value += 5, label += "!");
    return value;
}
function mutateOptional(value?: number) { if (value !== undefined) ++value; return value ?? 0; }
function ignored(value: number, extra?: number): number;
function ignored(value: number) { return value; }
function overloaded(value: number): number;
function overloaded(value: string, suffix?: string): string;
function overloaded(value: number | string, suffix = "!") {
    return typeof value === "number" ? value + 1 : value + suffix;
}
const counter = { value: 0 };
console.log(defaults(counter), defaults(counter, 0), defaults(counter, undefined, 2));
console.log(optional(), optional(0), optional(undefined), rawDefault(null), rawDefault(false));
console.log(overloaded(3), overloaded("hello"), overloaded("hello", "?"));
console.log(stringOptional(), stringOptional(""), boolOptional(), boolOptional(false));
console.log(refDefault().value, refDefault(counter) === counter);
console.log(mutateDefaults(), mutateOptional(), mutateOptional(3));
console.log(ignored(10, next(counter)), counter.value);
console.log(++counter.value, counter.value++, --counter.value, counter.value--, counter.value);
console.log(`line\n${undefined}:${null}:${true}:${-0}:${""}`);
function coalesce(left: any, right: any) { return left ?? right; }
console.log(coalesce(0, 9), coalesce(false, 9), coalesce("", 9), coalesce(null, 9), coalesce(undefined, 9));
console.log(coalesce(counter, { value: 99 }) === counter);
function lazy(counter: Counter, value: any) { return value ?? next(counter); }
console.log(lazy(counter, 0), lazy(counter, false), lazy(counter, null), lazy(counter, undefined), counter.value);
console.log(`${next(counter)}:${next(counter)}:${counter.value}`);

function loops() {
    let total = 0;
    outer: for (let i = 0; i < 4; ++i) {
        for (let j = 0; j < 4; ++j) {
            if (i === 3) break outer;
            if (j === 2) continue outer;
            total += 1;
        }
    }
    let n = 0;
    do {
        ++n;
        if (n < 3) continue;
        total += n;
    } while (n < 4);
    for (;;) { ++total; break; }
    for (n = 0; n < 3;) { ++n; if (n === 2) continue; total += n; }
    block: { total += 10; break block; total += 1000; }
    for (let i = 0, j = 0; i < 3; ++i, ++j) { total += j; }
    top: inner: for (let i = 0; i < 3; ++i) {
        if (i === 1) continue top;
        total += i;
    }
    return total;
}
console.log(loops());
function switches(value: number) {
    let checks = 0;
    let result = "";
    switch (value) {
        case ++checks: result += "one";
        default: result += "default";
        case ++checks: result += "two"; break;
        case ++checks: result += "three";
    }
    return `${checks}:${result}`;
}
console.log(switches(1), switches(2), switches(3), switches(9));
function mixed() {
    let result = 0;
    for (let i = 0; i < 5; ++i) {
        switch (i) { case 1: continue; case 3: break; default: result += i; }
        result += 10;
    }
    return result;
}
console.log(mixed());

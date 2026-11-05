type Options = { value: number | string; label?: string; flag?: boolean };
function update(options: Options) {
    console.log(Object.hasOwn(options, "value"), Object.hasOwn(options, "label"), Object.hasOwn(options, "flag"), options.label);
    options.value = "text";
    options.label = undefined;
    console.log(options.value, Object.hasOwn(options, "label"), options.label);
    options.label = "set";
    return options;
}
console.log(update({ value: 2 }).label);
console.log(update({ value: 3, label: undefined }).label);
const annotated: Options = { value: 4 };
annotated.value = "changed";
console.log(annotated.value, annotated.label, Object.hasOwn(annotated, "label"));
const explicit: Options = { value: 5, flag: undefined };
console.log(Object.hasOwn(explicit, "flag"), explicit.flag, Object.hasOwn(explicit, "label"));
function defaults(options: { count?: number; label?: string }) {
    console.log(Object.hasOwn(options, "count"), Object.hasOwn(options, "label"), options.count, options.label);
    options.count = undefined;
    console.log(Object.hasOwn(options, "count"), options.count);
}
defaults({});
function nested(options: { child: { value: number | string; missing?: number }; tag?: string }) {
    options.child.value = "nested";
    console.log(options.child.value, Object.hasOwn(options, "tag"), Object.hasOwn(options.child, "missing"));
    options.child.missing = undefined;
    console.log(Object.hasOwn(options.child, "missing"));
}
nested({ child: { value: 2 } });
function ordered(options: { first: number | string; second: number | string; absent?: number }) {
    options.first = "first";
    options.second = "second";
    console.log(options.first, options.second, Object.hasOwn(options, "absent"));
}
let sequence = 0;
function step() { sequence++; return sequence; }
ordered({ first: step(), second: step() });
console.log(sequence);
ordered({ second: step(), first: step() });
console.log(sequence);

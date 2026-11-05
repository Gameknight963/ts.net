function dynamicNumbers(seed: number) {
    let point = { value: seed };
    let view: any = point;
    const again: any = view;
    let total = 0;
    for (let i = 0; i < 100; i++) {
        again.value = view.value + 1;
        total += again.value as number;
    }
    console.log(total, point.value, typeof again.value);
}
dynamicNumbers(3);
function undefinedValue(initial?: number) {
    const point = { value: initial };
    const view: any = point;
    console.log(view.value, typeof view.value, view.value + 1);
    view.value = 4;
    view.value = view.value + 1;
    console.log(view.value, point.value);
    view.value = undefined;
    console.log(view.value, view.value + 1);
}
undefinedValue();
function concatenation(suffix: any) {
    const point = { value: 3 };
    const view: any = point;
    console.log(view.value + suffix, suffix + view.value);
}
concatenation("x");
concatenation(2);
function extraField() {
    const point = { value: 3 };
    const view: any = point;
    view.extra = 7;
    console.log(view.extra, view.value, view.missing);
}
extraField();
function rebind() {
    const point = { value: 1 };
    let view: any = point;
    view = { value: 9 };
    view.value = view.value + 1;
    console.log(point.value, view.value, point === view);
}
rebind();
function expose() {
    const point = { value: 5 };
    const view: any = point;
    return view;
}
console.log(expose().value);

function directAny() {
    let point: any = { value: 2 };
    const alias: any = point;
    alias.value = point.value + 3;
    console.log(point.value, typeof alias.value);
}
directAny();

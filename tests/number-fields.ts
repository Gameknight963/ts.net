const values: (number | undefined)[] = [undefined, NaN, -0, 0, Infinity, -Infinity, 1.5, 1.7976931348623157e308, 5e-324];
for (const value of values) {
    const record = {value};
    const dynamic: any = record;
    const copy = {...record};
    console.log(typeof record.value, record.value === undefined, Number.isNaN(record.value), record.value === value, record.value === 0 && 1 / record.value < 0);
    console.log(typeof dynamic.value, dynamic.value === undefined, Number.isNaN(dynamic.value), typeof copy.value, copy.value === undefined, Number.isNaN(copy.value));
    const wide: {value: unknown} = record;
    wide.value = undefined;
    console.log(record.value, Object.hasOwn(record, "value"));
    wide.value = -0;
    console.log(1 / (record.value as number));
}
function optional(source: {value?: number}) {
    const copy = {...source};
    console.log(Object.hasOwn(copy, "value"), copy.value);
    copy.value = NaN;
    console.log(Object.hasOwn(copy, "value"), Number.isNaN(copy.value), copy.value === undefined);
}
optional({});
optional({value: undefined});
optional({value: NaN});

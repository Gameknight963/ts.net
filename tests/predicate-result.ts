const check = Number.isFinite as unknown as (value: unknown) => number;
console.log(check(1));

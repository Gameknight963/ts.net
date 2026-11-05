function invoke(): boolean { return check(1); }
console.log(invoke());
const check = Number.isFinite;

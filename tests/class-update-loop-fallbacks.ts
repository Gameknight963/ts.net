export {};
class Counter {
    total = 1;
    empty(): void {}
    literal(): void {42;}
    early(): void {return;}
}
function emptyHelper(target: Counter): void {}
function earlyHelper(target: Counter): void {return;}
function literalHelper(target: Counter): number {return 42;}
function expressionHelper(target: Counter): void {42;}
function fallbacks(target: Counter, count: number): number {
    for (let i = 0; i < count; i++) {}
    for (let i = 0; i < count; i++);
    for (let i = 0; i < count; i++) emptyHelper(target);
    for (let i = 0; i < count; i++) earlyHelper(target);
    for (let i = 0; i < count; i++) literalHelper(target);
    for (let i = 0; i < count; i++) expressionHelper(target);
    for (let i = 0; i < count; i++) target.empty();
    for (let i = 0; i < count; i++) target.literal();
    for (let i = 0; i < count; i++) target.early();
    return target.total;
}
console.log(fallbacks(new Counter(), 40), fallbacks(new Counter(), 0));

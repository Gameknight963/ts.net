export {};
class Point {x = 0; y = 0; constructor(x: number, y: number) {this.x = x; this.y = y;}}
function points(): Point[] {return [new Point(1, 2), new Point(3, 4), new Point(5, 6)];}
function sum(items: Point[], seed: number): number {
    let total = seed;
    for (let i = 0; i < items.length; i++) total += items[i].x + items[i].y;
    return total;
}
function alias(items: Point[], seed: number): number {
    let total = seed;
    for (let i = 0; i < items.length; i++) {
        const item = items[i];
        total += item.x + item.y + i;
    }
    return total;
}
function caught(items: Point[], seed: number): number {
    let total = seed;
    try {
        for (let i = 0; i < items.length; i++) {
            const item = items[i];
            total += item.x + item.y;
        }
    } catch {return total;}
    return total;
}
function parameter(items: Point[], total: number): number {
    for (let i = 0; i < items.length; i++) total += items[i].x + items[i].y;
    return total;
}
for (const reduce of [sum, alias, caught, parameter]) {
    console.log('ordinary', reduce(points(), 17), reduce([], 17));
    const structural = points(); structural[2] = {x: 7, y: 8};
    console.log('structural tail', reduce(structural, 17));
    const frozen = points(); Object.freeze(frozen[2]);
    console.log('frozen tail', reduce(frozen, 17));
    const dynamic = points(); (dynamic[2] as any).x = undefined;
    console.log('undefined tail', reduce(dynamic, 17));
    let reads = 0;
    const accessor = points();
    Object.defineProperty(accessor[2], 'x', {get() {
        reads++; accessor[0].x = 100; accessor.push(new Point(10, 20)); return 5;
    }, configurable: true});
    console.log('getter tail', reduce(accessor, 17), reads, accessor[0].x, accessor.length);
    const throwing = points();
    Object.defineProperty(throwing[2], 'x', {get() {reads++; throw new Error('tail');}, configurable: true});
    try {console.log('throwing tail result', reduce(throwing, 17));}
    catch {console.log('throwing tail');}
    console.log('throwing reads', reads);
    const sparse = points(); delete sparse[2];
    const prototype = Object.create(Array.prototype);
    Object.defineProperty(prototype, '2', {get() {reads++; sparse[0].x = 200; return new Point(5, 6);}});
    Object.setPrototypeOf(sparse, prototype);
    console.log('inherited tail', reduce(sparse, 17), reads, sparse[0].x);
}
for (const reduce of [sum, alias, parameter]) {
    console.log('empty undefined seed', reduce([], undefined as any));
    console.log('empty negative zero', Object.is(reduce([], -0), -0));
    const structural = points(); structural[2] = {x: 7, y: 8};
    console.log('undefined seed restart', reduce(structural, undefined as any));
    console.log('negative zero restart', reduce(structural, -0));
}
function shifted(items: Point[]): number {
    let total = 17;
    for (let i = 2; i < items.length; i++) total += items[i].x + items[i].y;
    return total;
}
console.log('skip invalid prefix', shifted([null as any, undefined as any, new Point(5, 6)]));
console.log('short shifted', shifted([null as any]));
function visibleAlias(items: Point[]): string {
    let total = 17;
    for (let i = 0; i < items.length; i++) {
        var item = items[i];
        total += item.x + item.y;
    }
    return `${total}:${item!.x}`;
}
const visible = points(); visible[2] = {x: 7, y: 8};
console.log('visible fast alias', visibleAlias(points()));
console.log('visible alias', visibleAlias(visible));

class Flag {value = 1; active = true;}
function flags(items: Flag[]): number {
    let total = 17;
    for (let i = 0; i < items.length; i++) total += items[i].value + (items[i].active ? 3 : 0);
    return total;
}
const flagItems = [new Flag(), new Flag(), new Flag()];
flagItems[2].active = undefined as any;
console.log('undefined boolean tail', flags(flagItems));
flagItems[2] = {value: 7, active: false};
console.log('structural boolean tail', flags(flagItems));

class Choice {enabled = true; amount = 2; label = 'x';}
function conditional(items: Choice[]): number {
    let total = 17;
    for (let i = 0; i < items.length; i++)
        total += items[i].enabled ? items[i].amount : items[i].label.length;
    return total;
}
const choice = new Choice(); (choice as any).label = null;
const choices = [choice, {enabled: true, amount: 5, label: 'tail'}];
console.log('inactive invalid branch', conditional(choices));
choice.enabled = false;
try {conditional(choices);} catch (error) {console.log('prefix exception', (error as Error).name);}

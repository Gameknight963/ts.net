const seen: number[] = [];
const ancestor = {value: 100,
    take(value: number): number {return this.value + value;},
    record(value: number): void {seen.push(this.value + value);},
    text(value: number): string {return this.value + ':' + value;}
};
const child = {__proto__: ancestor, value: 10,
    take(value: number): number {return super.take(value);},
    record(value: number): void {super.record(value);},
    text(value: number): string {return super.text(value);}
};
console.log(child.take(2), child.text(3));
child.record(4);
const borrowed = {value: 20, take: child.take, record: child.record, text: child.text};
console.log(borrowed.take(5), borrowed.text(6));
borrowed.record(7);
Object.setPrototypeOf(child, {take(this: any, value: number): number {return this.value * 2 + value;}});
console.log(borrowed.take(8), Number.isNaN(borrowed.take(undefined as any)));
console.log(seen.join(','));

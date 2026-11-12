function emptyCopy(): {value?: number} {
    const source: {value?: number} = {};
    return {...source};
}
function write(target: {value?: number}, value: number, dynamic: boolean): void {
    const result = dynamic ? ((target as any).value = value) : (target.value = value);
    console.log('assigned', result);
}
for (const dynamic of [false, true]) {
    console.log('dynamic', dynamic);
    const setter = emptyCopy();
    let total = 0;
    Object.setPrototypeOf(setter, {set value(value: number) {
        console.log('setter receiver', this === setter);
        total += value;
    }});
    write(setter, 3, dynamic);
    write(setter, 5, dynamic);
    console.log(total, setter.value, Object.hasOwn(setter, 'value'));

    const getter = emptyCopy();
    Object.setPrototypeOf(getter, {get value(): number {return 7;}});
    try {write(getter, 11, dynamic);} catch {console.log('getter rejected');}
    console.log(getter.value, Object.hasOwn(getter, 'value'));

    const readonly = emptyCopy();
    const readonlyPrototype = {};
    Object.defineProperty(readonlyPrototype, 'value', {value: 13, writable: false});
    Object.setPrototypeOf(readonly, readonlyPrototype);
    try {write(readonly, 17, dynamic);} catch {console.log('readonly rejected');}
    console.log(readonly.value, Object.hasOwn(readonly, 'value'));

    const writable = emptyCopy();
    const writablePrototype = {value: 19};
    Object.setPrototypeOf(writable, writablePrototype);
    write(writable, 23, dynamic);
    console.log(writable.value, writablePrototype.value, Object.hasOwn(writable, 'value'));

    const locked = emptyCopy();
    Object.preventExtensions(locked);
    try {write(locked, 29, dynamic);} catch {console.log('extension rejected');}
    console.log(locked.value, Object.hasOwn(locked, 'value'));

    const own = {...{value: undefined as number | undefined}};
    Object.setPrototypeOf(own, {set value(value: number) {console.log('unexpected setter', value);}});
    write(own, 31, dynamic);
    console.log(own.value, Object.hasOwn(own, 'value'));

    const defining = emptyCopy();
    Object.setPrototypeOf(defining, {set value(value: number) {
        Object.defineProperty(this, 'value', {value: value + 1, writable: true, enumerable: true, configurable: true});
    }});
    write(defining, 37, dynamic);
    console.log(defining.value, Object.hasOwn(defining, 'value'));
}

const textSource: {text?: string} = {};
const textCopy = {...textSource};
Object.setPrototypeOf(textCopy, {set text(value: string) {console.log('text setter', value, this === textCopy);}});
textCopy.text = 'hello';
console.log(textCopy.text, Object.hasOwn(textCopy, 'text'));
function writeWide(target: {value?: number | string}, value: number | string): void {target.value = value;}
const narrow = emptyCopy();
Object.setPrototypeOf(narrow, {set value(value: number | string) {console.log('wide setter', value, this === narrow);}});
writeWide(narrow, 'wider');
console.log(narrow.value, Object.hasOwn(narrow, 'value'));

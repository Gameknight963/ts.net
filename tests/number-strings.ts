const samples: any[] = ['0x10', '0Xff', '0b101', '0O17', '\uFEFF 12\u2029', '\u00851', '-0', '1e2', 'infinity', '+0x1', '0b2', '.5'];
for (const text of samples) {
    const value = +text;
    console.log(value, value !== value, value === 0 && 1 / value < 0);
}
let hexadecimal: any = '0x10';
console.log(hexadecimal++, hexadecimal, hexadecimal *= ('0b10' as any));
const fields: any = {value: '0o10' as any};
console.log(fields.value **= 2, fields.value);



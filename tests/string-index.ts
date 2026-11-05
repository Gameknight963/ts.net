const array: any = [10, 20, 30];
const keys = ["", "0", "00", "01", "+1", "-0", "-1", " 1", "1 ", "1.0", "1e0", "1\u0000", "1\n", "?", "?", "4294967294", "4294967295", "4294967296", "9999999999", "10000000000"];
for (const key of keys) {
    array[key] = 7;
    console.log(array[key], Object.hasOwn(array, key), array.length);
}
console.log(array[0], array[1], array[2]);
const text: any = "abc";
for (const key of keys) console.log(text[key], Object.hasOwn(text, key));
const empty: any = "";
console.log(Object.hasOwn(empty, "0"), empty["0"], Object.hasOwn(empty, "length"));

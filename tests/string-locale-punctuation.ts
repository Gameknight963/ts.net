export {};
for (const ignorePunctuation of [false, true]) {
 for (const [a,b] of [["a-b","ab"],["hello!","hello"],["a b","ab"],["a_b","ab"],["a+b","ab"],["$a","a"],["a.b","ab"]]) {
  console.log(a,b,ignorePunctuation,Math.sign(a.localeCompare(b,"en",{ignorePunctuation})));
 }
}
console.log("combined", "A-2".localeCompare("a10","en",{ignorePunctuation:true,numeric:true,sensitivity:"base"}));
console.log("inherited", "a-b".localeCompare("ab","en",Object.create({ignorePunctuation:true})));

for (const [a,b] of [["a\tb","ab"],["a\u00a0b","ab"],["a\u2014b","ab"],["a\u3002b","ab"],["a\ud83d\ude00b","ab"],["a\ud800-b","a\ud800b"]]) console.log("unicode",Math.sign(a.localeCompare(b,"en",{ignorePunctuation:true})));

const options = Object.create({ignorePunctuation: true});
console.log("sort", ["a-b", "ac", "ab"].sort((a,b)=>a.localeCompare(b,"en",options)).join("|"));
const compare = String.prototype.localeCompare;
console.log("generic", compare.call(12, "1-2", "en", {ignorePunctuation:true}));
console.log("truthy", "a-b".localeCompare("ab","en",{ignorePunctuation:1 as any}));

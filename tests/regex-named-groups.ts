export {};
const expression=/(?<first>a)(b)(?<last>c)?/d;
for (const text of ["ab","abc"]) {
 const match=expression.exec(text)!;
 console.log("captures",match.join("|"),match.groups!.first,match.groups!.last,Object.keys(match.groups!).join("|"),Object.getPrototypeOf(match.groups!)===null);
 console.log("indices",JSON.stringify(match.indices),JSON.stringify(match.indices!.groups),match.indices![1]===match.indices!.groups!.first);
}
console.log("refs",/(?<a>a)(b)\1\2\k<a>/.test("ababa"));
console.log("replace","abc".replace(expression,"$<last>-$2-$<first>-$<missing>"));
console.log("absent","ab".replace(expression,"$<last>:$1:$<first>"));
console.log("literal","ab".replace(/(a)b/,"$<first>"));
console.log("callback","abc".replace(expression,(...args:any[])=>{console.log(args.length,args[1],args[2],args[3],args[4],args[5],args[6].first,args[6].last);return args[6].last;}));
for (const match of "ab abc".matchAll(/(?<first>a)(b)(?<last>c)?/gd)) console.log("all",match.index,match.join("|"),JSON.stringify(match.groups));
console.log("split","abcabc".split(/(?<a>a)(b)/).join("|"));
console.log("collision",/(?<tsnetCapture1>a)(b)/.exec("ab")!.join("|"));

console.log("nested",/(?<outer>a(?<inner>b))(c)/.exec("abc")!.join("|"));
console.log("lookbehind",/(?<=(?<before>a))(?<after>b)(c)/.exec("abc")!.join("|"));
const unusual=/(?<__proto__>a)(?<constructor>b)/.exec("ab")!.groups!;
console.log("keys",Object.keys(unusual).join("|"),unusual.__proto__,unusual.constructor,Object.getPrototypeOf(unusual)===null);
console.log("forward",/\k<a>(?<a>a)/.test("a"));

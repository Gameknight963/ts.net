function show(pattern:RegExp,text:string):void{const result=pattern.exec(text);console.log(JSON.stringify(result),JSON.stringify(result?.indices),result?.indices?.groups,pattern.hasIndices,pattern.flags);}
show(/(a)(b)?/d,"xa");show(/(a)(b)?/d,"xab");show(/(a)(b)?/d,"none");show(/()/d,"");show(/(a)+/d,"aaa");show(/a(?=(b))/d,"ab");
show(/(.)/d,"\ud83d\ude00");show(/(a).(b)/ds,"a\nb");
const global=/(a)(b)?/dg;const matches=[..."ab a".matchAll(global)];for(const match of matches)console.log("all",match.index,JSON.stringify(match.indices));
const plain="xab".match(/(a)(b)/d);console.log("match",JSON.stringify(plain?.indices));
const noFlag=/a/.exec("a")!;console.log("absent",noFlag.indices,Object.hasOwn(noFlag,"indices"));
const result=/(a)/d.exec("a")!;console.log("keys",Object.keys(result).join("|"),Object.keys(result.indices!).join("|"));
const clone=new RegExp(/a/d);console.log("clone",clone.flags,clone.hasIndices,String(clone));

export {};
const words=["iglesia", "Iglesia", "hilo", "HI", "hi", "hielo", "HIA", "hiato", "hierba", "hi\ud83d\ude00", "i\nx", "h\u0130", "\u0130tem", "\u0131tem", "\u00edtem", "ocho", "OCHO", "horas", "HO", "8", "80", "11", "11 gatos", "11 ", "11.5", "110", "11gatos", "11\tx", "11\nx", "11\u00a0x", "", "\u05d4", "\u05d0a", "a\u05d0", " \u05d9\u05e9", "\u0591", "\u0590", "\ufb1d", "\ud83d\ude00"];
for(const locale of ['es','es-MX','es-419','he','he-IL'])for(const type of ['conjunction','disjunction','unit'])for(const style of ['long','short','narrow']) {
    const format=new Intl.ListFormat(locale,{type,style} as Intl.ListFormatOptions);
    for(const word of words)for(const prefix of [[],['A'],['A','B'],['A','B','C']]) {
        const values=[...prefix,word];
        console.log(locale,type,style,JSON.stringify(values),JSON.stringify(format.format(values)),JSON.stringify(format.formatToParts(values)));
    }
}
const hebrew=new Intl.ListFormat('he');
for(let point=0x590;point<=0x5ff;point++)console.log(point,JSON.stringify(hebrew.format(['A',String.fromCodePoint(point)])));
for(let point=0xfb00;point<=0xfb50;point++)console.log(point,JSON.stringify(hebrew.format(['A',String.fromCodePoint(point)])));

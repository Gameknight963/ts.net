const segmenter=new Intl.Segmenter('en-US',{granularity:'grapheme'});
for(const text of ['', 'abc','a\u0301b','\ud83d\udc69\u200d\ud83d\udcbb!','\ud83c\uddfa\ud83c\uddf8\ud83c\udde8\ud83c\udde6','\r\nX','\u1100\u1161\u11a8','\ud800x']) {
 const segments=segmenter.segment(text);
 console.log('segments',JSON.stringify(Array.from(segments).map(part=>[part.segment,part.index,part.input===text,'isWordLike' in part])));
 for(const index of [-1,0,1,2,3,4,1.9,NaN,Infinity]) {const part=segments.containing(index);console.log('at',String(index),part?JSON.stringify([part.segment,part.index]):'none');}
 const first=segments[Symbol.iterator](),second=segments[Symbol.iterator]();console.log('independent',JSON.stringify(first.next()),JSON.stringify(second.next()));
}
console.log('metadata',Intl.Segmenter.name,Intl.Segmenter.length,segmenter.segment.name,segmenter.segment.length,segmenter.resolvedOptions.name,segmenter.resolvedOptions.length);
console.log('options',JSON.stringify(segmenter.resolvedOptions()));
console.log('supported',Intl.Segmenter.supportedLocalesOf(['en-US','de-DE','en-US','zz']).join('|'));
console.log('tags',Object.prototype.toString.call(segmenter),Object.prototype.toString.call(segmenter.segment('a')[Symbol.iterator]()));
class Custom extends Intl.Segmenter {marker=3;}
const child=new Custom('en-US');console.log('derived',child.marker,child instanceof Custom,child instanceof Intl.Segmenter,Array.from(child.segment('a\u0301')).length);
for(const method of [Intl.Segmenter.prototype.segment,Intl.Segmenter.prototype.resolvedOptions])try{(method as any).call({},'a');}catch(error){console.log('receiver',(error as Error).name);}
for(const options of [{granularity:'invalid'},{localeMatcher:'invalid'}])try{new Intl.Segmenter('en-US',options as any);}catch(error){console.log('invalid',(error as Error).name);}
let reads=0;const cached=new Intl.Segmenter('en-US',{get granularity(){reads++;return 'grapheme' as const;}});cached.segment('a');cached.segment('b');console.log('cached',reads);

const parts=segmenter.segment('a\u0301b');const iterator=parts[Symbol.iterator]();
console.log('self-iterator',iterator[Symbol.iterator]()===iterator);
while(!iterator.next().done){}console.log('done',iterator.next().done,iterator.next().done);
try{(parts.containing as any).call({},0);}catch(error){console.log('containing-receiver',(error as Error).name);}
try{(iterator.next as any).call({});}catch(error){console.log('iterator-receiver',(error as Error).name);}
console.log('locale-keyword',new Intl.Segmenter('en-US-u-kf-upper').resolvedOptions().locale);

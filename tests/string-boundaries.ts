const text = 'abcabc';
console.log('basic', text.startsWith('abc'), text.endsWith('abc'), text.startsWith('bc'), text.endsWith('ab'));
for(const position of [undefined, null, 0, 1, 3, 6, 99, -1, -Infinity, Infinity, NaN, 3.9]) {
    console.log('positions', position, text.startsWith('abc', position as any), text.endsWith('abc', position as any), text.startsWith('', position as any), text.endsWith('', position as any));
}
console.log('long', 'a'.startsWith('aa'), 'a'.endsWith('aa'), ''.startsWith('a'), ''.endsWith('a'));
console.log('unicode', '\uD83D\uDE00'.startsWith('\uD83D'), '\uD83D\uDE00'.endsWith('\uDE00'), '\uD800'.startsWith('\uD800'));
console.log('ordinal', 'A'.startsWith('a'), 'e\u0301'.endsWith('\u00e9'));
const starts: any = text.startsWith, ends: any = text.endsWith, concat: any = ''.concat;
console.log('coercion', starts.call('undefined'), ends.call('null', null), starts.call(123, 12), ends.call(true, 'ue'));
console.log('boxed', new String('abc').startsWith('a'), new String('abc').endsWith('c'), new String('a').concat('b'));
console.log('concat', ''.concat(), 'a'.concat('b', 'c', 'd'), concat.call(1, null, undefined, true));
let effects = '';
const receiver = {toString():string {effects += 'S';return 'abc';}};
const search = {toString():string {effects += 'N';return 'a';}};
const position = {valueOf():number {effects += 'P';return 0;}};
console.log('order', starts.call(receiver, search, position), effects);
effects = '';
console.log('empty-position', starts.call('abc', '', position), effects);
effects = '';
console.log('concat-order', concat.call(receiver, {toString():string {effects += 'A';return 'x';}}, {toString():string {effects += 'B';return 'y';}}), effects);
try {starts.call(null, search, position);} catch(error) {console.log('null', error instanceof TypeError);}
try {ends.call(undefined, 'x');} catch(error) {console.log('undefined', error instanceof TypeError);}
try {concat.call(null, 'x');} catch(error) {console.log('concat-null', error instanceof TypeError);}
try {starts.call('a', {toString():string {throw 'search failure';}}, position);} catch(error) {console.log('search-throw', error);}
try {ends.call('a', 'a', {valueOf():number {throw 'position failure';}});} catch(error) {console.log('position-throw', error);}
try {concat.call('a', {toString():string {throw 'concat failure';}});} catch(error) {console.log('concat-throw', error);}
console.log('metadata', starts.name, starts.length, ends.name, ends.length, concat.name, concat.length);
console.log('bound', starts.bind('abc')('a'), ends.apply('abc', ['c']), concat.apply('x', ['y', 'z']));
const original = String.prototype.startsWith;
String.prototype.startsWith = () => false;
console.log('override', 'abc'.startsWith('a'));
String.prototype.startsWith = original;
console.log('restore', 'abc'.startsWith('a'), Object.keys(String.prototype).length);
let laterConversion = false;
try {concat.call('a', {toString():string {throw 'first';}}, {toString():string {laterConversion = true;return 'later';}});} catch(error) {console.log('concat-stops', error, laterConversion);}
const mutation = {toString():string {return 'abc';}};
console.log('receiver-snapshot', starts.call(mutation, {toString():string {mutation.toString = () => 'changed';return 'a';}}));
console.log('extra-arguments', starts.call('abc', 'a', 0, {valueOf():number {throw 'unused';}}));
console.log('search-object', starts.call('abc', {toString():string {return 'a';}}), ends.call('abc', new String('c')));

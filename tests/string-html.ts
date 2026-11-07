const names = ['anchor','big','blink','bold','fixed','fontcolor','fontsize','italics','link','small','strike','sub','sup'];
for (const name of names) {
    const method: any = (String.prototype as any)[name];
    console.log('metadata', method.name, method.length, Object.prototype.propertyIsEnumerable.call(String.prototype, name));
    for (const text of ['', 'text', '<&>"\'', '\uD800\u0000\uDC00']) {
        for (const value of [undefined, null, 'x"y"&<>\'', 3, false]) {
            const result = method.call(text, value);
            const codes: number[] = [];
            for (let i = 0; i < result.length; i++) codes.push(result.charCodeAt(i));
            console.log(name, codes.join(','));
        }
    }
    console.log('boxed', method.call(Object('boxed'), 'value'));
    console.log('number', method.call(42, 'value'));
    console.log('omitted', method.call('text'));
    let effects = '';
    const receiver = {toString(): string { effects += 'R'; return 'receiver'; }};
    const argument = {toString(): string { effects += 'A'; return 'attribute"'; }};
    console.log('coercion', method.call(receiver, argument), effects);
    effects = '';
    try { method.call(null, argument); } catch (error) { console.log('null', (error as Error).name, effects); }
    try { method.call(undefined, argument); } catch (error) { console.log('undefined', (error as Error).name, effects); }
    const badArgument = {toString(): string { effects += 'V'; throw new Error('attribute failure'); }};
    try { console.log('attribute ignored', method.call('text', badArgument)); }
    catch (error) { console.log('attribute failure', (error as Error).message, effects); }
    const broken = {toString(): string { effects += 'T'; throw new Error('receiver failure'); }};
    try { method.call(broken, argument); } catch (error) { console.log('receiver failure', (error as Error).message, effects); }
}
console.log('direct', 'text'.bold(), 'text'.anchor('x"'), 'text'.fontsize(3), 'text'.link('x'));

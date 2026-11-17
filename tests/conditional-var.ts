export {};
function conditional(flag: boolean): void {
    if (flag) { var integer: number | undefined = 3; var text: string | undefined = 'text'; var bigint: bigint | undefined = 4n; }
    console.log('conditional', integer, text, bigint);
}
conditional(false); conditional(true);
function loops(): void {
    for (var index = 0; index < 0; index++) { var body: number | undefined = 7; }
    console.log('loops', index, body);
    while (false) { var text: string | undefined = 'unused'; }
    console.log('while', text);
}
loops();
function caught(): void {
    try { throw new Error('stop'); var value: object | undefined = {}; } catch {}
    console.log('caught', value);
}
caught();
function forward(): void {
    console.log('forward', value);
    var value: number | undefined = 9;
    console.log('after', value);
}
forward();
function property(flag: boolean): void {
    if (flag) { var value: any = { count: 11 }; }
    const alias = value;
    try { console.log('property', alias.count); } catch (error) { console.log('property', (error as Error).name); }
}
property(false); property(true);

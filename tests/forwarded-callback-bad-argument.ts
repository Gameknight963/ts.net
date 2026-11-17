function read(value: number): number { return value + 1; }
function forward(callback: (value: number) => number, value: any, extra: number): number { return callback(value); }
let evaluated = false;
function last(): number { evaluated = true; return 3; }
try { forward(read, 'bad', last()); }
catch (error) {
    if (!evaluated) throw new Error('Callback check ran before the last argument');
    throw error;
}

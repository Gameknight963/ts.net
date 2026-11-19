export {};
for (const base of [Symbol, BigInt]) {
    let declared = false;
    try {
        class Ordinary extends (base as any) {}
        declared = true;
        console.log(base.name, 'declared', Object.getPrototypeOf(Ordinary) === base);
        try { new Ordinary(); } catch (error) { console.log('super', error instanceof TypeError); }
    } catch (error) { console.log('declaration failed', declared, error instanceof TypeError); }
    const replacement = {value: 42};
    try {
        class Replaced extends (base as any) {
            constructor() { if (false) super(); return replacement; }
        }
        console.log(base.name, 'replacement', new Replaced() === replacement);
    } catch { console.log('replacement failed'); }
}

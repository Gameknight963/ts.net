let guardedEffects = 0;
function guardedWrite(): void {
    for (let index = 0; index < 1; index++) { guardedEffects++; guardedValues[0] = index; }
}
function guardedRead(): number {
    let total = 0;
    for (let index = 0; index < 1; index++) { guardedEffects++; total += guardedValues[index]; }
    return total;
}
function guardedLength(): void {
    for (let index = 0; index < guardedValues.length; index++) guardedEffects++;
}
function guardedSkip(): void {
    for (let index = 0; index < 1; index++) { guardedEffects++; if (index > 2) guardedValues[index] = index; }
}
function guardedReturn(): number {
    for (let index = 0; index < 1; index++) {
        guardedEffects++;
        if (index === 0) return 17;
        guardedValues[index] = index;
    }
    return 19;
}
try { guardedWrite(); } catch (error) { console.log("global write", (error as Error).name, guardedEffects); }
try { guardedRead(); } catch (error) { console.log("global read", (error as Error).name, guardedEffects); }
try { guardedLength(); } catch (error) { console.log("condition read", (error as Error).name, guardedEffects); }
try { guardedSkip(); console.log("global skip", guardedEffects); } catch { console.log("unexpected global skip"); }
try { console.log("global return", guardedReturn(), guardedEffects); } catch { console.log("unexpected global return"); }
const guardedValues = [1, 2].map(value => value);
guardedWrite();
console.log("global ready", guardedRead(), guardedEffects, guardedValues.join(","));

function capturedBindings(): void {
    let effects = 0;
    function write(): void {
        for (let index = 0; index < 1; index++) { effects++; values[0] = index; }
    }
    function read(): number {
        let total = 0;
        for (let index = 0; index < 1; index++) { effects++; total += values[index]; }
        return total;
    }
    function dense(): number {
        let total = 0;
        for (let index = 0; index < 1; index++) { effects++; total += mixed[index] as number; }
        return total;
    }
    function tags(): number {
        let total = 0;
        for (let index = 0; index < 1; index++) { effects++; if (typeof mixed[index] === "number") total++; }
        return total;
    }
    try { write(); } catch (error) { console.log("captured write", (error as Error).name, effects); }
    try { read(); } catch (error) { console.log("captured read", (error as Error).name, effects); }
    try { dense(); } catch (error) { console.log("captured dense", (error as Error).name, effects); }
    try { tags(); } catch (error) { console.log("captured tags", (error as Error).name, effects); }
    const values = [3, 4].map(value => value);
    const mixed: any[] = [7, "text"];
    write();
    console.log("captured ready", read(), dense(), tags(), effects);
}
capturedBindings();

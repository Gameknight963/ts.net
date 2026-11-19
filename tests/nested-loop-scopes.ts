export {};
function run(flag: boolean): void {
    console.log('forward', value);
    if (flag) { var value: number | undefined = 7; }
    const callbacks: (() => number)[] = [];
    for (let i = 0; i < 3; i++) {
        let sum = 0;
        for (let j = 0; j < 2; j++) sum += i + j;
        callbacks.push(() => sum + i);
    }
    console.log(value, callbacks.map(callback => callback()).join(','));
    const numbers = [1, 2, 3];
    let total = 0;
    for (let i = 0; i < numbers.length; i++) {
        function change(): void { if (i === 1) numbers[i] = 10; }
        change();
        total += numbers[i]!;
    }
    console.log(total);
    function nested(): void {
        console.log('nested forward', value);
        var value: number | undefined = 9;
        for (let i = 0; i < 2; i++) console.log('nested', i, value);
    }
    nested();
}
run(false); run(true);

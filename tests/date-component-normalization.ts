export {};
const base = new Date(2024, 0, 1).getTime();
for (const days of [10000000, -10000000, 100000000, -100000000]) {
    try {
        const value = new Date(2024, 0, 1 + days, -days * 24);
        console.log('cancel', days, value.getTime() === base, value.getFullYear(), value.getMonth(), value.getDate());
    } catch (error) {console.log('failure', (error as Error).name);}
}
const rolled = new Date(2024, 0, 1, 24 * 366);
console.log('roll', rolled.getFullYear(), rolled.getMonth(), rolled.getDate());
for (const hours of [10000000, -10000000]) {
    const date = new Date(base);
    console.log('setter', date.setHours(hours, -hours * 60) === base, date.getTime() === base);
}

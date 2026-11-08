export {};
for (const year of [9998, 9999]) {
    for (const month of [0, 11]) {
        const date = new Date(2000, month, month === 0 ? 1 : 31, month === 0 ? 0 : 23, 59, 59, 999);
        date.setFullYear(year);
        console.log('local', year, month, date.getTime(), date.toISOString());
        console.log('utc', date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate(), date.getUTCHours());
    }
}
for (const hour of [0, 12, 23]) {
    const date = new Date(9999, 11, 31, hour, 59, 59, 999);
    console.log('constructed', hour, date.getTime(), date.toISOString());
}

export {};
const date = new Date(Date.UTC(2024, 0, 1));
for (const numberingSystem of ['latn', 'deva', 'fullwide', 'mathbold']) for (const timeZone of ['UTC', 'Asia/Kolkata', 'America/St_Johns']) for (const timeZoneName of ['shortOffset', 'longOffset'] as const) {
    const options: Intl.DateTimeFormatOptions = {numberingSystem, timeZone, timeZoneName, hour: '2-digit', minute: '2-digit'};
    console.log(numberingSystem, timeZone, timeZoneName, new Intl.DateTimeFormat('en-US', options).format(date).replace(/\s/g, ' '));
    console.log('date', date.toLocaleTimeString('en-US', options).replace(/\s/g, ' '));
}

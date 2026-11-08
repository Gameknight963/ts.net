export {};
const date = new Date(Date.UTC(2024, 0, 1, 0, 4));
for (const timeZone of ['+05:30', '-0330', '+09', '+00', '-00:00', '+14:01', '+23:59', '-23:59']) {
    for (const timeZoneName of ['shortOffset', 'longOffset', 'short', 'long'] as const) {
        const options: Intl.DateTimeFormatOptions = {timeZone, timeZoneName, hour: '2-digit', minute: '2-digit', hourCycle: 'h23'};
        const format = new Intl.DateTimeFormat('en-US', options);
        console.log(timeZone, timeZoneName, format.resolvedOptions().timeZone, format.format(date).replace(/\s/g, ' '), date.toLocaleTimeString('en-US', options).replace(/\s/g, ' '));
    }
    console.log('date', timeZone, date.toLocaleDateString('en-US', {timeZone, year: 'numeric', month: '2-digit', day: '2-digit'}));
}
for (const timeZone of ['+24', '-24:00', '+01:60', '+1', '+010', '+01:02:03', '+ab:cd', '+01::00']) {
    try {new Intl.DateTimeFormat('en-US', {timeZone});}
    catch (error) {console.log('invalid', timeZone, (error as Error).name);}
}

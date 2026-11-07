for (const [locale,calendar] of [["ja-JP","japanese"],["th-TH","gregory"],["he-IL","hebrew"],["ar-SA","gregory"],["fa-IR","gregory"]]) {
    const options:Intl.DateTimeFormatOptions={calendar,timeZone:"UTC",year:"numeric",month:"numeric",day:"numeric"};
    const original=new Intl.DateTimeFormat(locale,{timeZone:"UTC"}).resolvedOptions().calendar;
    const formatter=new Intl.DateTimeFormat(locale,options);
    const date=new Date("2024-02-29T12:00:00Z");
    console.log(locale,formatter.resolvedOptions().calendar,formatter.format(date).length>0);
    console.log(formatter.format(date)===date.toLocaleDateString(locale,options));
    console.log(new Intl.DateTimeFormat(locale,{timeZone:"UTC"}).resolvedOptions().calendar===original);
}

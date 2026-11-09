export {};
for(const locale of ['en-US','th-TH'])for(const calendar of ['chinese','dangi','gregory','iso8601']) {
    const format=new Intl.DateTimeFormat(locale,{calendar,numberingSystem:'latn',timeZone:'UTC',year:'numeric',month:'2-digit',day:'2-digit'});
    for(const end of [Date.UTC(2024,1,29),Date.UTC(2024,2,1),Date.UTC(2025,2,1)]) {
        const text=format.formatRange(Date.UTC(2024,1,29),end);
        console.log(locale,calendar,text.replace(/\s+/g,' '));
    }
}

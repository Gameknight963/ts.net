/// <reference lib="esnext.intl" />
export {};
for(const region of ['US','GB','FR','JP','CN','CX','HK','MO','SG','TW','KR','SA','EG','IR','AF','TH','ET','IL','IN',
    'BD','DJ','DZ','EH','ER','ID','IQ','JO','KM','LB','LY','MA','MR','MY','NE','OM','PK','PS','SD','SY','TD','TN','YE',
    'AE','BH','KW','QA','AL','AZ','MV','TJ','TM','TR','UZ','XK','001','ZZ']) {
    const locale=new Intl.Locale('und-'+region);
    console.log('region',region,locale.getCalendars().join(','));
    const first=locale.getCalendars();first.reverse();first.push('changed');
    console.log('fresh',locale.getCalendars().join(','));
}
for(const tag of ['ja','zh-Hant','fa','th','am','ar','qaa','und','fa-US','en-US-u-rg-thzzzz','en-TH-u-rg-uszzzz','en-u-sd-cn11','en-US-u-sd-cn11','en-TH-u-rg-zzzzzz','en-TH-u-rg-xxzzzz','en-u-rg-001zzzz','ar-EG-u-rg-sazzzz','en-u-rg-irzzzz-sd-th10']) {
    console.log('locale',tag,new Intl.Locale(tag).getCalendars().join(','));
}
for(const calendar of ['japanese','gregory','islamic-civil','ethioaa','unknown']) {
    const locale=new Intl.Locale('en-US-u-rg-thzzzz',{calendar});
    console.log('restricted',locale.calendar,locale.getCalendars().join(','));
}

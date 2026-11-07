// CLR combined date/time patterns omit Intl's comma in en-US.
console.log(new Date(0).toLocaleString("en-US",{timeZone:"UTC"}).replace(",", ""));
console.log(new Date(0).toLocaleDateString("en-US",{timeZone:"UTC",calendar:"gregory",numberingSystem:"latn"}));

for (const value of [0n, 1n, -1n, 123456789012345678901234567890n, -123456789012345678901234567890n]) {
    for (const locale of ["en-US", "de-DE", "en-GB", "ja-JP", "hi-IN"]) {
        console.log("locale", value.toLocaleString(locale), Object(value).toLocaleString(locale));
        console.log("ungrouped", value.toLocaleString(locale, {useGrouping: false}));
        console.log("fraction", value.toLocaleString(locale, {minimumFractionDigits: 2, maximumFractionDigits: 4}));
    }
}
console.log("locales", 123456789n.toLocaleString(["de-DE", "en-US"]));
console.log("array", [1234567890123456789n, -5n].toLocaleString("en-US"));
try {BigInt.prototype.toLocaleString.call(1 as any);} catch (error) {console.log("receiver", (error as Error).name);}
console.log("metadata", BigInt.prototype.toLocaleString.name, BigInt.prototype.toLocaleString.length);

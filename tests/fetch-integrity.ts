export {};
const origin = 'http://127.0.0.1:0';
async function run(): Promise<void> {
 const cases: [string, string][] = [
  [
    "sha256",
    "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ="
  ],
  [
    "sha384",
    "sha384-WeF0h3dEjGnea4ANejO7+5/xtGPkQ1TDVTvNucZm+pASWjx5+QOXvfX2oT3oKGhP"
  ],
  [
    "sha512",
    "sha512-m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw=="
  ],
  [
    "mismatch",
    "sha256-iBCtWB5Z8rw5KLJhcHpxMI9+E56wSCA2bcTVwY2YAiU="
  ],
  [
    "strongest mismatch",
    "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ= sha512-SoDN1KTIIw7BrNLOO2E5gZ6RT0203EbsYh0K3YjV4wVLQ4NZusWZ/B4QHaOenS/iO5/dViWJP2p5+YIScDRiKg=="
  ],
  [
    "strongest match",
    "sha256-iBCtWB5Z8rw5KLJhcHpxMI9+E56wSCA2bcTVwY2YAiU= sha512-m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw=="
  ],
  [
    "alternative",
    "sha512-SoDN1KTIIw7BrNLOO2E5gZ6RT0203EbsYh0K3YjV4wVLQ4NZusWZ/B4QHaOenS/iO5/dViWJP2p5+YIScDRiKg== sha512-m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw=="
  ],
  [
    "reverse alternative",
    "sha512-m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw== sha512-SoDN1KTIIw7BrNLOO2E5gZ6RT0203EbsYh0K3YjV4wVLQ4NZusWZ/B4QHaOenS/iO5/dViWJP2p5+YIScDRiKg=="
  ],
  [
    "unpadded",
    "sha512-m3HSJL1i83hdltRq0+o9czGb+8KJDKra4t/3JRlnPKcjI8PZm6XBHXx6zG4UuMXaDEZjR1wuXDre9G9zvN7AQw"
  ],
  [
    "options",
    "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=?future"
  ],
  [
    "unknown",
    "sha999-nonsense"
  ],
  [
    "whitespace",
    "\tsha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=\n sha384-WeF0h3dEjGnea4ANejO7+5/xtGPkQ1TDVTvNucZm+pASWjx5+QOXvfX2oT3oKGhP\r\f"
  ],
  [
    "case",
    "SHA256-nonsense"
  ],
  [
    "url alphabet",
    "sha256-LPJNul-wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ="
  ],
  [
    "extra padding",
    "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ===="
  ],
  [
    "space separated",
    " sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ= "
  ],
  [
    "unknown stronger",
    "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ= sha999-invalid"
  ],
  [
    "missing digest",
    "sha512"
  ],
  [
    "empty digest",
    "sha512-"
  ],
  [
    "bad characters",
    "sha256-!!!"
  ],
  [
    "wrong size",
    "sha256-YQ=="
  ],
  [
    "changed digest case",
    "sha256-lpjnUL+WOW4M6dSQXBNINHSwhLWFP0jECWqZyPolMcq="
  ],
  [
    "stronger before weaker",
    "sha512-AA== sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ="
  ]
];
 for (const [name, integrity] of cases) {
  try { const response = await fetch(origin + '/text', {integrity}); console.log(name, await response.text()); }
  catch (error) { console.log(name, (error as Error).name); }
 }
 const compressed = await fetch(origin + '/gzip', {integrity: "sha256-naMIwuS8M6+nLfXAiLX8VnPEd/PvIda9qjWDk4NPmAQ="});
 console.log('decoded bytes', await compressed.text());
 const redirected = await fetch(new Request(origin + '/redirect', {integrity: "sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ="}));
 console.log('redirect', redirected.redirected, await redirected.text());
 try { const empty = await fetch(origin + '/empty', {integrity: "sha256-47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU="});
 console.log('empty', empty.status, empty.body === null, await empty.text()); } catch (error) { console.log('empty', (error as Error).name); }
}
run();

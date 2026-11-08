export {};
for (const a of 'abcdefghijklmnopqrstuvwxyz') for (const b of 'abcdefghijklmnopqrstuvwxyz') {
 const tag = a + b; const locale = new Intl.Locale(tag);
 console.log(tag, locale.maximize().toString(), locale.minimize().toString());
}
for (const tag of ['und', 'zz', 'en-GB', 'zh-TW', 'az-IR', 'pa-PK', 'sr-ME', 'und-Arab', 'und-GB', 'und-Latn-RU', 'und-Arab-IN', 'und-Latn-CN', 'zh-Hant-HK', 'en-Zzzz', 'en-ZZ', 'und-Zzzz-ZZ', 'en-Zzzz-ZZ', 'und-Qaaa', 'en-Qaaa', 'zz-Arab', 'sl-rozaj-biske', 'en-u-ca-gregory-hc-h12-nu-deva', 'de-DE-x-private']) {
 const locale = new Intl.Locale(tag);
 console.log('locale', tag, locale.maximize().toString(), locale.minimize().toString(), locale.toString());
}
for (const name of ['maximize', 'minimize']) {
 const method = Intl.Locale.prototype[name as 'maximize' | 'minimize'];
 console.log('metadata', name, method.name, method.length);
 for (const receiver of [null, undefined, {}, 'en-US', Intl.Locale.prototype]) {
  try {method.call(receiver);}
  catch (error) {console.log('receiver', name, (error as Error).name);}
 }
}
class DerivedLocale extends Intl.Locale {}
const derived = new DerivedLocale('en');
console.log('returned class', derived.maximize() instanceof Intl.Locale, derived.maximize() instanceof DerivedLocale, derived.minimize() !== derived);

for (const a of 'abcdefghijklmnopqrstuvwxyz') for (const b of 'abcdefghijklmnopqrstuvwxyz') for (const c of 'abcdefghijklmnopqrstuvwxyz') {
 const tag = a + b + c; const locale = new Intl.Locale(tag);
 console.log('three letters', tag, locale.maximize().toString(), locale.minimize().toString());
}
for (const a of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') for (const b of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
 const tag = 'und-' + a + b; const locale = new Intl.Locale(tag);
 // ICU sometimes retains und for regional tags; verify the equivalent round trip.
 console.log('region', tag, locale.maximize().toString(), locale.minimize().maximize().toString());
}

for (const tag of ['sl-rozaj-biske', 'de-1901', 'en-oxendict', 'sl-rozaj-rozaj', 'en-abcd-abcde-abcdefghi']) {
 try {console.log('variants', tag, new Intl.Locale(tag).toString(), Intl.getCanonicalLocales(tag).join(','));}
 catch (error) {console.log('variant error', tag, (error as Error).name);}
}
for (const tag of ['hy-SU', 'az-SU', 'az-Arab-SU', 'tpi-PC', 'cnr-CS']) {
 const locale = new Intl.Locale(tag);
 console.log('alias context', tag, locale.maximize().toString(), locale.minimize().toString());
}

for (const script of "Adlm Aghb Ahom Arab Armi Armn Avst Bali Bamu Bass Batk Beng Berf Bhks Bopo Brah Brai Bugi Buhd Cakm Cans Cari Cham Cher Chrs Copt Cprt Cyrl Deva Diak Dogr Dupl Egyp Elba Elym Ethi Gara Geor Glag Gong Gonm Goth Gran Grek Gujr Gukh Guru Hanb Hang Hani Hano Hans Hant Hatr Hebr Hira Hluw Hmng Hmnp Hung Ital Jamo Java Jpan Kali Kana Kawi Khar Khmr Khoj Kits Knda Kore Krai Kthi Lana Laoo Latn Lepc Limb Lina Linb Lisu Lyci Lydi Mahj Maka Mand Mani Marc Medf Mend Merc Mero Mlym Modi Mong Mroo Mtei Mult Mymr Nagm Nand Narb Nbat Newa Nkoo Ogam Olck Onao Orkh Orya Osge Osma Ougr Palm Pauc Perm Phag Phli Phlp Phnx Plrd Prti Rjng Rohg Runr Samr Sarb Saur Sgnw Shaw Shrd Sidd Sidt Sind Sinh Sogd Sogo Sora Soyo Sund Sunu Sylo Syrc Tagb Takr Tale Talu Taml Tang Tavt Tayo Telu Tfng Tglg Thaa Thai Tibt Tirh Tnsa Todr Tols Toto Tutg Ugar Vaii Vith Wara Wcho Xpeo Xsux Yezi Yiii Zanb Zzzz".split(' ')) for (const region of ['', '-CN', '-GB', '-IN', '-RU']) {
 const tag = 'und-' + script + region; const locale = new Intl.Locale(tag);
 console.log('script', tag, locale.maximize().toString(), locale.minimize().maximize().toString());
}

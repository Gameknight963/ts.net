export {};
async function run(): Promise<void> {
 const urls = [
  'data:,hello', 'data:text/plain,hello%20world', 'data:;charset=utf-8,%E2%98%83',
  'data:application/octet-stream,%00%FF%80%25', 'data:,a+b%2Bc', 'data:,a?b#fragment',
  'data:,bad%2G%0%', 'data:;base64,aGVsbG8=', 'data:text/plain; BASE64,aGVs%20bG8',
  'data:;base64,', 'data:;base64,abcde', 'data:;base64,!!!!', 'data:missing',
  'data:TEXT/PLAIN;Charset=UTF-8;X="a b";x=ignored,hello',
  'data:invalid;foo=bar,hello', 'data:text/plain;broken;foo=bar,hello',
  'data:text/plain;empty="";quoted="a; b",hello', 'data:;charset=latin1;BaSe64,aGVsbG8=',
  'data:text/plain;base64=not-encoding,aGVsbG8=', 'data:text/plain;foo="unterminated,hello'
 ];
 for (let i=0; i<urls.length; i++) {
  try {
   const response = await fetch(urls[i]); const bytes = new Uint8Array(await response.arrayBuffer());
   console.log(i,response.status,response.statusText,response.type,response.redirected,response.headers.get('content-type'),Array.from(bytes).join(','));
  } catch (error) { console.log(i,(error as Error).name); }
 }
 for (const method of ['GET','HEAD','POST','PUT','DELETE']) {
  const response = await fetch('data:,hello',{method}); console.log('method',method,response.body===null,await response.text());
 }
 const response = await fetch('data:,hello#fragment'); console.log('url',response.url);
 try { response.headers.set('x-test','value'); } catch(error) { console.log('immutable',(error as Error).name); }
 const controller = new AbortController(); const reason = new Error('cancel');
 const pending = fetch('data:,hello',{signal:controller.signal}); controller.abort(reason);
 try { await pending; } catch (error) { console.log('abort',error===reason); }
 for (const url of ['DATA:,hello', 'data:,snowman \u2603', 'data:,a%20b', 'data:text/plain;X=\"a b\",hello']) {
  const request = new Request(url); const response = await fetch(request);
  console.log('serialization',request.url,response.url,await response.text());
 }
 const cloneable = await fetch('data:,copy'); console.log('clone',await cloneable.clone().text(),await cloneable.text());
 for (const integrity of ['sha256-LPJNul+wow4m6DsqxbninhsWHlwfp0JecwQzYpOLmCQ=', 'sha256-AA==']) {
  try {console.log('integrity',await (await fetch('data:,hello',{integrity})).text());}catch(error){console.log('integrity',(error as Error).name);}
 }
}
run();

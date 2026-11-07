async function run() {
 const events: string[] = ['start'];
 await new Promise<void>(resolve => {
  setTimeout((a: string, b: number) => { events.push(a + b); Promise.resolve().then(() => events.push('after first')); }, 0, 'first ', 2);
  setTimeout(() => { events.push('second'); queueMicrotask(() => { events.push('microtask'); resolve(); }); }, 0);
  const cancelled = setTimeout(() => events.push('cancelled'), 0); clearTimeout(cancelled);
  const cross = setInterval(() => events.push('cross cancelled'), 1); clearTimeout(cross);
  const crossTimeout = setTimeout(() => events.push('cross timeout'), 1); clearInterval(crossTimeout);
  queueMicrotask(() => events.push('initial microtask'));
  Promise.resolve().then(() => events.push('initial promise'));
 });
 console.log(events.join('|'));
 let ticks = 0;
 await new Promise<void>(resolve => {
  const interval = setInterval((amount: number) => { ticks += amount; if (ticks === 6) { clearInterval(interval); resolve(); } }, 1, 2);
 });
 console.log('interval', ticks);
 const value = await new Promise<number>(resolve => setTimeout(() => resolve(7), 2)); console.log('await', value);
 let nested = 0;
 await new Promise<void>(resolve => {
  function again() { nested++; if (nested < 3) setTimeout(again, 0); else resolve(); }
  setTimeout(again, 0);
 });
 console.log('nested', nested);
 const alias = setTimeout;
 console.log('alias', await new Promise<number>(resolve => alias(resolve, 0, 9)));
 const long = setTimeout(() => console.log('wrong'), 60000); clearTimeout(long);
 clearTimeout(999999); clearInterval(undefined);
 const start = performance.now(); await new Promise<void>(resolve => setTimeout(resolve, 20));
 console.log('waited', performance.now() - start >= 10);
 let calls = 0;
 await new Promise<void>(resolve => {
  const interval = setInterval(() => { calls++; queueMicrotask(() => { clearInterval(interval); resolve(); }); }, 0);
 });
 console.log('microtask cancellation', calls);
 try { setTimeout(3 as any, 0); } catch (error) { console.log('invalid callback', error instanceof TypeError); }
}
run();

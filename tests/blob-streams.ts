export {};
async function main(): Promise<void> {
 const blob = new Blob(["abc"]);
 const first = blob.stream(), second = blob.stream();
 console.log("streams", first !== second, first.locked, second.locked);
 const reader = first.getReader();
 console.log("locked", first.locked);
 try { first.getReader(); } catch (error) { console.log("second reader", (error as Error).name); }
 try { await first.cancel(); } catch (error) { console.log("locked cancel", (error as Error).name); }
 const chunk = await reader.read();
 console.log("read", chunk.done, Array.from(chunk.value!).join(","));
 chunk.value!.fill(0);
 console.log("blob unchanged", await blob.text());
 console.log("finished", (await reader.read()).done);
 await reader.closed;
 reader.releaseLock();
 console.log("released", first.locked);
 const independent = second.getReader();
 console.log("independent", Array.from((await independent.read()).value!).join(","));
 independent.releaseLock();
 const cancelled = blob.stream(); await cancelled.cancel("stop");
 console.log("cancelled", (await cancelled.getReader().read()).done, await blob.text());
 console.log("empty", (await new Blob().stream().getReader().read()).done);
 let text = "";
 for await (const bytes of blob.stream()) text += String.fromCharCode(...bytes);
 console.log("iteration", text);
 const response = new Response(blob.stream());
 console.log("response", await response.text(), await blob.text());
 class Child extends Blob {}
 console.log("subclass", Array.from((await new Child(["x"]).stream().getReader().read()).value!).join(","));
 console.log("file", await new Response(new File(["file"], "name.txt").stream()).text());
 const input = new Uint8Array(150000); input.fill(65);
 const large = new Blob([input]); input.fill(0);
 let count = 0, sum = 0;
 for await (const bytes of large.stream()) {
  count += bytes.length;
  for (const value of bytes) sum += value;
 }
 console.log("large", count, sum);
 const detached = blob.stream;
 try { detached.call({} as Blob); } catch (error) { console.log("receiver", (error as Error).name); }
}
main().catch(error => console.log("failure", String(error)));

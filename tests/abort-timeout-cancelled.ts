export {};
AbortSignal.timeout(0).addEventListener("abort",()=>console.log("abort"));const cancelled=setTimeout(()=>console.log("should-not-run"),100);clearTimeout(cancelled);console.log("done");

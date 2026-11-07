try{
 var constructor=(JsFunction)Js.BuiltinNamespace("SharedArrayBuffer");var integers=(JsFunction)Js.BuiltinNamespace("Int32Array");var bytes=(JsFunction)Js.BuiltinNamespace("Uint8Array");
 var buffer=constructor.Construct(new object[]{16d});var clone=((JsFunction)Js.BuiltinNamespace("structuredClone")).InvokeWithReceiver(Js.Undefined,new object[]{buffer});
 var first=integers.Construct(new[]{buffer});var second=integers.Construct(new[]{clone});var byteView=bytes.Construct(new[]{buffer});
 var atomics=(TsObject)Js.BuiltinNamespace("Atomics");var add=(JsFunction)atomics.Get("add");var load=(JsFunction)atomics.Get("load");var store=(JsFunction)atomics.Get("store");
 const int workers=8,iterations=50000;
 Parallel.For(0,workers,worker=>{var view=worker%2==0?first:second;for(int index=0;index<iterations;index++)add.InvokeWithReceiver(atomics,new object[]{view,0d,1d});});
 if((double)load.InvokeWithReceiver(atomics,new object[]{first,0d})!=workers*iterations)throw new Exception("Lost updates through cloned shared buffers");
 store.InvokeWithReceiver(atomics,new object[]{first,0d,0d});
 Parallel.For(0,workers,worker=>{for(int index=0;index<iterations;index++)add.InvokeWithReceiver(atomics,worker%2==0?new object[]{byteView,BitConverter.IsLittleEndian?0d:3d,1d}:new object[]{second,0d,256d});});
 double expected=(workers/2*iterations)*256d+(workers/2*iterations)%256;
 if((double)load.InvokeWithReceiver(atomics,new object[]{first,0d})!=expected)throw new Exception("Overlapping integer widths did not share an atomic lock");
 var lockFree=(JsFunction)atomics.Get("isLockFree");foreach(double size in new[]{1d,2d,4d,8d})if((bool)lockFree.InvokeWithReceiver(atomics,new object[]{size}))throw new Exception("Lock-backed atomics were reported as lock-free");
 var wait=(JsFunction)atomics.Get("wait");var notify=(JsFunction)atomics.Get("notify");
 store.InvokeWithReceiver(atomics,new object[]{first,0d,0d});
 void Wake(object waitingView,object expectedValue){
  var task=Task.Run(()=>(string)wait.InvokeWithReceiver(atomics,new object[]{waitingView,0d,expectedValue,5000d}));
  var clock=System.Diagnostics.Stopwatch.StartNew();double count=0;
  while(count==0 && clock.Elapsed<TimeSpan.FromSeconds(5)){count=(double)notify.InvokeWithReceiver(atomics,new object[]{second,0d,1d});if(count==0)Thread.Sleep(1);}
  if(count!=1 || task.GetAwaiter().GetResult()!="ok")throw new Exception("Notify did not wake a waiter through the cloned storage");
 }
 Wake(first,0d);Wake(((JsFunction)Js.BuiltinNamespace("BigInt64Array")).Construct(new[]{buffer}),System.Numerics.BigInteger.Zero);
 int successful=0,notifications=0;
 var waiting=Task.WhenAll(Enumerable.Range(0,2).Select(_=>Task.Run(()=>{for(int index=0;index<200;index++){string status=(string)wait.InvokeWithReceiver(atomics,new object[]{first,0d,0d,5d});if(status=="ok")Interlocked.Increment(ref successful);else if(status!="timed-out")throw new Exception("Unexpected wait result");}})));
 var deadline=System.Diagnostics.Stopwatch.StartNew();
 while(!waiting.IsCompleted && deadline.Elapsed<TimeSpan.FromSeconds(10)){notifications+=(int)(double)notify.InvokeWithReceiver(atomics,new object[]{second,0d,1d});Thread.Sleep(1);}
 if(!waiting.IsCompleted)throw new Exception("Wait stress did not finish");waiting.GetAwaiter().GetResult();
 if(notifications!=successful)throw new Exception("A notification was lost at the timeout boundary");
 Console.WriteLine("PASS cross-wrapper and cross-width wakeups and concurrent timeout/notify accounting");
 Console.WriteLine("PASS 800000 concurrent updates through cloned buffers and overlapping integer widths; lock-free query is honest");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}

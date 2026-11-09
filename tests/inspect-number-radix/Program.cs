using System.Globalization;
using System.Numerics;
var method=(JsFunction)Js.Get(0d,"toString");
var decimalScale=BigInteger.Pow(10,1200);
void Check(double value,int radix) {
 string text=(string)method.InvokeWithReceiver(value,new object[]{(double)radix});
 if(text.Length>1100)throw new Exception("Unbounded radix output");
 bool negative=text.StartsWith('-');string magnitude=negative ? text[1..] : text;
 int point=magnitude.IndexOf('.'),places=point<0 ? 0 : magnitude.Length-point-1;
 if(magnitude.Count(ch=>ch=='.')>1)throw new Exception("Repeated radix point");
 BigInteger numerator=0;
 foreach(char ch in magnitude){if(ch=='.')continue;int digit="0123456789abcdefghijklmnopqrstuvwxyz".IndexOf(ch);if(digit<0 || digit>=radix)throw new Exception("Invalid radix digit");numerator=numerator*radix+digit;}
 // Independently parse through 1200 decimal places and the CLR's binary64
 // parser. This also covers subnormals without iterative floating-point loss.
 BigInteger denominator=BigInteger.Pow(radix,places);
 string exact=(numerator*decimalScale/denominator).ToString(CultureInfo.InvariantCulture).PadLeft(1201,'0');exact=exact.Insert(exact.Length-1200,".");
 double roundtrip=double.Parse((negative ? "-" : "")+exact,NumberStyles.Float,CultureInfo.InvariantCulture);
 if(BitConverter.DoubleToUInt64Bits(roundtrip)!=BitConverter.DoubleToUInt64Bits(value))throw new Exception($"Radix {radix} failed roundtrip for {value:R}: {text}");
}
var values=new List<double>{double.Epsilon,Math.BitIncrement(double.Epsilon),Math.BitDecrement(BitConverter.UInt64BitsToDouble(0x0010000000000000)),BitConverter.UInt64BitsToDouble(0x0010000000000000),Math.BitIncrement(BitConverter.UInt64BitsToDouble(0x0010000000000000)),double.MaxValue,Math.BitDecrement(double.MaxValue),0.1,0.2,0.3,Math.PI,1d/3,1e20,1e100,1e-100,9007199254740992d};
var random=new Random(73);
for(int i=0;i<80;i++){ulong bits=(ulong)random.NextInt64()&0x7fefffffffffffffUL;if(bits!=0)values.Add(BitConverter.UInt64BitsToDouble(bits));}
int cases=0;
for(int radix=3;radix<=36;radix++)if(radix!=10 && (radix&(radix-1))!=0)foreach(double value in values){Check(value,radix);Check(-value,radix);cases+=2;}
Console.WriteLine($"PASS {cases} exact radix roundtrips through the CLR decimal parser");

using System;
using System.Numerics;
using System.Text;

public static partial class Js
{
    // Scale binary64 values by 2^1075 so even the smallest subnormal's
    // neighbouring midpoints can be represented using integer arithmetic.
    static BigInteger ScaledRadixNumber(ulong bits)
    {
        int exponent=(int)(bits>>52);ulong significand=bits&0x000fffffffffffffUL;
        if(exponent!=0)significand|=0x0010000000000000UL;
        return new BigInteger(significand)<<(exponent==0 ? 1 : exponent);
    }
    static bool RadixCandidate(BigInteger lower,BigInteger upper,BigInteger value,BigInteger unit,bool inclusive,out BigInteger candidate)
    {
        var floor=BigInteger.DivRem(value,unit,out var remainder);
        bool Allowed(BigInteger number) {
            var scaled=number*unit;
            return (scaled>lower || inclusive && scaled==lower) && (scaled<upper || inclusive && scaled==upper);
        }
        bool roundUp=remainder*2>unit || remainder*2==unit && !floor.IsEven;
        candidate=roundUp ? floor+1 : floor;
        if(Allowed(candidate))return true;
        candidate=roundUp ? floor : floor+1;return Allowed(candidate);
    }
    static string RadixIntegerDigits(BigInteger number,int radix)
    {
        const string digits="0123456789abcdefghijklmnopqrstuvwxyz";
        var reversed=new StringBuilder();
        do{number=BigInteger.DivRem(number,radix,out var digit);reversed.Append(digits[(int)digit]);}while(!number.IsZero);
        var result=new char[reversed.Length];for(int i=0;i<result.Length;i++)result[i]=reversed[result.Length-i-1];return new string(result);
    }
    // Choose the closest shortest spelling as recommended by ECMA-262
    // Number::toString, Note 2. V8 can choose different final digits.
    static string RoundedNumberRadix(double number,int radix)
    {
        ulong bits=BitConverter.DoubleToUInt64Bits(Math.Abs(number));
        var value=ScaledRadixNumber(bits);var previous=ScaledRadixNumber(bits-1);
        // The overflow boundary above the greatest finite number has the same
        // spacing as its predecessor; there is no finite next value to decode.
        var next=bits==0x7fefffffffffffffUL ? value*2-previous : ScaledRadixNumber(bits+1);
        var lower=value+previous;var upper=value+next;value*=2;
        var denominator=BigInteger.One<<1076;bool inclusive=(bits&1)==0;
        int places=0;string text;
        if(RadixCandidate(lower,upper,value,denominator,inclusive,out var digits)) {
            var unit=denominator;
            while(RadixCandidate(lower,upper,value,unit*radix,inclusive,out var shorter)){unit*=radix;places++;digits=shorter;}
            text=RadixIntegerDigits(digits,radix)+new string('0',places);
        } else {
            do {
                if(++places>1076)throw new InvalidOperationException("Radix conversion did not converge");
                lower*=radix;upper*=radix;value*=radix;
            }while(!RadixCandidate(lower,upper,value,denominator,inclusive,out digits));
            text=RadixIntegerDigits(digits,radix).PadLeft(places+1,'0');text=text.Insert(text.Length-places,".");
        }
        return number<0 ? "-"+text : text;
    }
}

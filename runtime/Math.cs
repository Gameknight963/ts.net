using System;
using System.Collections.Generic;
using System.Numerics;

public static partial class Js
{
    static double MathRound(double value)
    {
        if (!double.IsFinite(value) || value == 0) return value;
        if (value < 0 && value >= -0.5) return -0d;
        double floor = Math.Floor(value);
        return value - floor < 0.5 ? floor : floor + 1;
    }
    static double MathExpm1(double value)
    {
        if (value == 0) return value;
        if (Math.Abs(value) > 0.5 || double.IsNaN(value)) return Math.Exp(value) - 1;
        double term = value, result = value;
        for (int n = 2; n <= 20; n++) { term *= value / n; result += term; }
        return result;
    }
    static double MathLog1p(double value)
    {
        if (value == 0 || double.IsPositiveInfinity(value)) return value;
        double sum = 1 + value;
        if (sum == 1) return value;
        if (sum <= 0) return Math.Log(sum);
        return Math.Log(sum) - ((sum - 1) - value) / sum;
    }
    static double MathExtreme(object[] arguments, bool maximum)
    {
        double result = maximum ? double.NegativeInfinity : double.PositiveInfinity;
        foreach (object argument in arguments)
        {
            double value = Number(argument);
            // Continue converting later arguments even after observing NaN.
            if (double.IsNaN(value) || double.IsNaN(result)) result = double.NaN;
            else if (maximum ? value > result : value < result) result = value;
            else if (value == 0 && result == 0)
                result = maximum ? (double.IsNegative(value) && double.IsNegative(result) ? -0d : 0d) : (double.IsNegative(value) || double.IsNegative(result) ? -0d : 0d);
        }
        return result;
    }
    static double MathHypot(object[] arguments)
    {
        var values = new double[arguments.Length];
        double scale = 0; bool nan = false;
        for (int i = 0; i < values.Length; i++)
        {
            double value = Math.Abs(Number(arguments[i])); values[i] = value;
            if (double.IsNaN(value)) nan = true;
            else scale = Math.Max(scale, value);
        }
        if (double.IsPositiveInfinity(scale)) return scale;
        if (nan) return double.NaN;
        if (scale == 0) return 0;
        double sum = 0, compensation = 0;
        foreach (double value in values)
        {
            double normalized = value / scale;
            double term = normalized * normalized - compensation;
            double next = sum + term;
            compensation = (next - sum) - term; sum = next;
        }
        return scale * Math.Sqrt(sum);
    }
    sealed class MathNamespace : TsObject
    {
        readonly HashSet<string> hidden = new(), constants = new();
        void Member(string name, object value, bool constant = false) { DefineOwn(name, value); hidden.Add(name); if (constant) constants.Add(name); }
        void Unary(string name, Func<double, double> operation) => Member(name, new JsFunction((FunctionBody)(args => operation(Number(FunctionArgument(args, 0)))), (RawNumberFunction1)(value => operation(value)), name, 1));
        void Binary(string name, Func<double, double, double> operation) => Member(name, new JsFunction((FunctionBody)(args => operation(Number(FunctionArgument(args, 0)), Number(FunctionArgument(args, 1)))), (RawNumberFunction2)((first, second) => operation(first, second)), name, 2));
        internal MathNamespace()
        {
            Member("E", Math.E, true); Member("PI", Math.PI, true);
            Member("LN2", Math.Log(2), true); Member("LN10", Math.Log(10), true);
            Member("LOG2E", Math.Log2(Math.E), true); Member("LOG10E", Math.Log10(Math.E), true);
            Member("SQRT1_2", Math.Sqrt(0.5), true); Member("SQRT2", Math.Sqrt(2), true);
            Unary("abs", Math.Abs); Unary("ceil", Math.Ceiling); Unary("floor", Math.Floor); Unary("trunc", Math.Truncate); Unary("round", MathRound);
            Unary("sign", static value => value == 0 || double.IsNaN(value) ? value : value > 0 ? 1 : -1);
            Unary("sqrt", Math.Sqrt); Unary("cbrt", Math.Cbrt); Unary("exp", Math.Exp); Unary("expm1", MathExpm1);
            Unary("log", Math.Log); Unary("log1p", MathLog1p); Unary("log2", Math.Log2); Unary("log10", Math.Log10);
            Unary("sin", Math.Sin); Unary("cos", Math.Cos); Unary("tan", Math.Tan);
            Unary("asin", Math.Asin); Unary("acos", Math.Acos); Unary("atan", Math.Atan);
            Unary("sinh", Math.Sinh); Unary("cosh", Math.Cosh); Unary("tanh", Math.Tanh);
            Unary("asinh", Math.Asinh); Unary("acosh", Math.Acosh); Unary("atanh", Math.Atanh);
            Unary("fround", static value => (double)(float)value);
            Unary("clz32", static value => BitOperations.LeadingZeroCount(unchecked((uint)ToInt32(value))));
            Binary("pow", Power); Binary("atan2", Math.Atan2);
            Binary("imul", static (first, second) => unchecked(ToInt32(first) * ToInt32(second)));
            Member("min", new JsFunction((FunctionBody)(static args => MathExtreme(args, false)), null, "min", 2));
            Member("max", new JsFunction((FunctionBody)(static args => MathExtreme(args, true)), null, "max", 2));
            Member("hypot", new JsFunction((FunctionBody)MathHypotBody, null, "hypot", 2));
            Member("random", new JsFunction((FunctionBody)(static _ => Random.Shared.NextDouble()), (RawNumberFunction0)(static () => Random.Shared.NextDouble()), "random", 0));
        }
        static object MathHypotBody(object[] arguments) => MathHypot(arguments);
        internal override bool IsReadOnlyOwnProperty(string key) => constants.Contains(key);
        public override object Set(string key, object value) => constants.Contains(key) ? throw new InvalidOperationException("Math constant is read-only") : base.Set(key, value);
        public override bool DeleteOwn(string key) { bool removed = base.DeleteOwn(key); if (removed) hidden.Remove(key); return removed; }
        internal override bool ForInEnumerable(string key) => !hidden.Contains(key) && base.ForInEnumerable(key);
        internal override object ForInOwnKeys() => base.OwnKeys();
        internal override object OwnPropertyNames() => base.OwnKeys();
        public override object OwnKeys()
        {
            var keys = (JsArray)base.OwnKeys(); var result = new JsArray(0);
            for (uint i = 0; i < keys.Length(); i++) { string key = (string)keys.ElementAt(i); if (!hidden.Contains(key)) result.Append(key); }
            return result;
        }
    }
}

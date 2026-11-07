public static partial class Js
{
    static class ClassDefinitionFunctions
    {
        internal static readonly JsFunction Key = new((FunctionBody)(static args => PropertyKey(FunctionArgument(args,0))), null, "", 1);
        internal static readonly JsFunction Method = new((FunctionBody)(static args => {
            DefineClassMethod(FunctionArgument(args,0), PropertyKey(FunctionArgument(args,1)), FunctionArgument(args,2), Number(FunctionArgument(args,3)) != 0, (int)Number(FunctionArgument(args,4)));
            return Undefined;
        }), null, "", 5);
        internal static readonly JsFunction Accessor = new((FunctionBody)(static args => DefineAutoAccessor(FunctionArgument(args,0), PropertyKey(FunctionArgument(args,1)), Number(FunctionArgument(args,2)) != 0)), null, "", 3);
        internal static readonly JsFunction Private = new((FunctionBody)(static args => DefinePrivateMember(FunctionArgument(args,0), StringValue(FunctionArgument(args,1)), (int)Number(FunctionArgument(args,2)), FunctionArgument(args,3), FunctionArgument(args,4))), null, "", 5);
    }
}

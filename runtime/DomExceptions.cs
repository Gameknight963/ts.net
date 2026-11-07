using System;
using System.Collections.Generic;

public static partial class Js
{
    static readonly string[] DomConstantNames =
    {
        "INDEX_SIZE_ERR", "DOMSTRING_SIZE_ERR", "HIERARCHY_REQUEST_ERR", "WRONG_DOCUMENT_ERR", "INVALID_CHARACTER_ERR",
        "NO_DATA_ALLOWED_ERR", "NO_MODIFICATION_ALLOWED_ERR", "NOT_FOUND_ERR", "NOT_SUPPORTED_ERR", "INUSE_ATTRIBUTE_ERR",
        "INVALID_STATE_ERR", "SYNTAX_ERR", "INVALID_MODIFICATION_ERR", "NAMESPACE_ERR", "INVALID_ACCESS_ERR",
        "VALIDATION_ERR", "TYPE_MISMATCH_ERR", "SECURITY_ERR", "NETWORK_ERR", "ABORT_ERR", "URL_MISMATCH_ERR",
        "QUOTA_EXCEEDED_ERR", "TIMEOUT_ERR", "INVALID_NODE_TYPE_ERR", "DATA_CLONE_ERR"
    };
    // Obsolete error names retain code 0, while the legacy constants keep their original values.
    static readonly Dictionary<string, int> DomErrorCodes = new(StringComparer.Ordinal)
    {
        ["IndexSizeError"]=1,["HierarchyRequestError"]=3,["WrongDocumentError"]=4,["InvalidCharacterError"]=5,
        ["NoModificationAllowedError"]=7,["NotFoundError"]=8,["NotSupportedError"]=9,["InUseAttributeError"]=10,
        ["InvalidStateError"]=11,["SyntaxError"]=12,["InvalidModificationError"]=13,["NamespaceError"]=14,["InvalidAccessError"]=15,
        ["TypeMismatchError"]=17,["SecurityError"]=18,["NetworkError"]=19,["AbortError"]=20,["URLMismatchError"]=21,
        ["QuotaExceededError"]=22,["TimeoutError"]=23,["InvalidNodeTypeError"]=24,["DataCloneError"]=25
    };
    sealed class DomExceptionValue(string message, string name) : JsCaughtError("Error", "", false)
    {
        internal readonly string Message = message, Name = name;
        internal int Code => DomErrorCodes.TryGetValue(Name, out int code) ? code : 0;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.DOMException.Get("prototype");
    }
    static bool DomConstant(string key) => Array.IndexOf(DomConstantNames, key) >= 0;
    sealed class DomExceptionPrototype : JsClassInstance
    {
        internal override bool IsReadOnlyOwnProperty(string key) => DomConstant(key) || base.IsReadOnlyOwnProperty(key);
        internal override bool OwnPropertyConfigurable(string key) => !DomConstant(key) && base.OwnPropertyConfigurable(key);
        public override bool DeleteOwn(string key) => !DomConstant(key) && base.DeleteOwn(key);
    }
    static DomExceptionValue RequireDomException(object value) => value as DomExceptionValue ?? throw new InvalidOperationException("DOMException receiver required");
    sealed class DomExceptionConstructor : NativeNamespaceFunction
    {
        internal DomExceptionConstructor() : base("DOMException", static _ => throw new InvalidOperationException("DOMException requires new"), 0)
        {
            var prototype = new DomExceptionPrototype { ParentPrototype = (TsObject)((JsFunction)GetErrorConstructor("Error")).Get("prototype") }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "DOMException");
            prototype.DefineAccessor("name", new CollectionMethod("get name", 0, static (receiver, _) => RequireDomException(receiver).Name), 1);
            prototype.DefineAccessor("message", new CollectionMethod("get message", 0, static (receiver, _) => RequireDomException(receiver).Message), 1);
            prototype.DefineAccessor("code", new CollectionMethod("get code", 0, static (receiver, _) => (double)RequireDomException(receiver).Code), 1);
            for (int i = 0; i < DomConstantNames.Length; i++)
            {
                DefineOwn(DomConstantNames[i], (double)(i + 1));
                prototype.DefineOwn(DomConstantNames[i], (double)(i + 1));
            }
        }
        internal override bool IsReadOnlyOwnProperty(string key) => DomConstant(key) || base.IsReadOnlyOwnProperty(key);
        internal override bool OwnPropertyConfigurable(string key) => !DomConstant(key) && base.OwnPropertyConfigurable(key);
        public override bool DeleteOwn(string key) => !DomConstant(key) && base.DeleteOwn(key);
        public override object Construct(object[] args) => new DomExceptionValue(IsUndefined(FunctionArgument(args,0)) ? "" : StringValue(args[0]), IsUndefined(FunctionArgument(args,1)) ? "Error" : StringValue(args[1]));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result=(TsObject)Construct(args);result.SetPrototype(((TsObject)instance).PropertyPrototype);return result; }
    }
}

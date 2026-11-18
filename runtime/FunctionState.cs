using System;

public partial class JsFunction
{
    // Ordinary methods and callbacks need none of this state. An unobserved
    // constructor prototype needs only a flag, not a separate allocation.
    sealed class FunctionState
    {
        internal Delegate Numeric;
        internal FunctionBody Factory;
        internal object Prototype;
    }

    enum FunctionStateKind : byte {None, Numeric, Factory, Prototype, Combined}

    // A single optional value occupies this slot directly. A container is
    // needed only when a function uses more than one of these features.
    object functionState;
    FunctionStateKind functionStateKind;
    bool hasOrdinaryPrototype;

    FunctionState EnsureFunctionState()
    {
        if (functionStateKind == FunctionStateKind.Combined) return (FunctionState)functionState;
        var state = new FunctionState {
            Numeric = functionStateKind == FunctionStateKind.Numeric ? (Delegate)functionState : null,
            Factory = functionStateKind == FunctionStateKind.Factory ? (FunctionBody)functionState : null,
            Prototype = functionStateKind == FunctionStateKind.Prototype ? functionState : null
        };
        functionState = state;
        functionStateKind = FunctionStateKind.Combined;
        return state;
    }

    object ordinaryPrototype
    {
        get => !hasOrdinaryPrototype ? null : functionStateKind == FunctionStateKind.Prototype ? functionState :
            functionStateKind == FunctionStateKind.Combined ? ((FunctionState)functionState).Prototype ?? UncreatedPrototype : UncreatedPrototype;
        set
        {
            hasOrdinaryPrototype = value != null;
            object prototype = ReferenceEquals(value, UncreatedPrototype) ? null : value;
            if (functionStateKind is FunctionStateKind.None or FunctionStateKind.Prototype) {
                functionState = prototype;
                functionStateKind = prototype == null ? FunctionStateKind.None : FunctionStateKind.Prototype;
            }
            else if (prototype != null) EnsureFunctionState().Prototype = prototype;
            else if (functionStateKind == FunctionStateKind.Combined) ((FunctionState)functionState).Prototype = null;
        }
    }

    internal FunctionBody constructFactory
    {
        get => functionStateKind == FunctionStateKind.Factory ? (FunctionBody)functionState :
            functionStateKind == FunctionStateKind.Combined ? ((FunctionState)functionState).Factory : null;
        set
        {
            if (functionStateKind is FunctionStateKind.None or FunctionStateKind.Factory) {
                functionState = value;
                functionStateKind = value == null ? FunctionStateKind.None : FunctionStateKind.Factory;
            }
            else if (value != null) EnsureFunctionState().Factory = value;
            else if (functionStateKind == FunctionStateKind.Combined) ((FunctionState)functionState).Factory = null;
        }
    }
}

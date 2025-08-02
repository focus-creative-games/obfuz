using System;

namespace Obfuz.ObfusPasses
{
    [Flags]
    public enum ObfuscationPassType
    {
        None = 0,

        ConstEncrypt = 0x1,
        FieldEncrypt = 0x2,

        SymbolObfus = 0x100,
        CallObfus = 0x200,
        ExprObfus = 0x400,
        ControlFlowObfus = 0x800,
        EvalStackObfus = 0x1000,

        RemoveConstField = 0x100000,
        WaterMark = 0x200000,

        AllObfus = SymbolObfus | CallObfus | ExprObfus | ControlFlowObfus | EvalStackObfus,
        AllEncrypt = ConstEncrypt | FieldEncrypt,

        MethodBodyObfusOrEncrypt = ConstEncrypt | CallObfus | ExprObfus | ControlFlowObfus | EvalStackObfus,

        All = ~0,
    }
}

using System;

namespace Obfuz.ObfusPasses
{
    [Flags]
    public enum ObfuscationPassType
    {
        None = 0,

        ConstEncryption = 0x1,
        MemoryEncryption = 0x2,

        SymbolObfuscation = 0x100,
        CallProxy = 0x200,
        ExprObfuscation = 0x400,
        ControlFlowObfuscation = 0x800,


        AllDataEncryption = ConstEncryption | MemoryEncryption,
        AllCodeObfuscation = SymbolObfuscation | CallProxy | ExprObfuscation | ControlFlowObfuscation,
        All = AllDataEncryption | AllCodeObfuscation,
    }
}

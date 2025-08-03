using dnlib.DotNet;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    interface IObfuscator
    {
        bool Obfuscate(MethodDef method, ObfusMethodContext ctx);
    }

    abstract class ObfuscatorBase : IObfuscator
    {
        public abstract bool Obfuscate(MethodDef method, ObfusMethodContext ctx);
    }
}

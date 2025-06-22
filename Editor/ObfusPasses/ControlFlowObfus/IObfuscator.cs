using Obfuz.Emit;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    interface IObfuscator
    {
        bool Obfuscate(BasicBlockCollection basicBlocks, ObfusMethodContext ctx);
    }

    abstract class ObfuscatorBase : IObfuscator
    {
        public abstract bool Obfuscate(BasicBlockCollection basicBlocks, ObfusMethodContext ctx);
    }
}

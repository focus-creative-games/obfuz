using Obfuz.Emit;

namespace Obfuz
{
    public abstract class ObfuscationPassBase : IObfuscationPass
    {
        public abstract void Start(ObfuscatorContext ctx);

        public abstract void Stop(ObfuscatorContext ctx);

        public abstract void Process(ObfuscatorContext ctx);
    }
}

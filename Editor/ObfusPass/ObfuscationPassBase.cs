using Obfuz.Emit;

namespace Obfuz
{
    public abstract class ObfuscationPassBase : IObfuscationPass
    {
        public abstract void Start(ObfuscationPassContext ctx);

        public abstract void Stop(ObfuscationPassContext ctx);

        public abstract void Process(ObfuscationPassContext ctx);
    }
}

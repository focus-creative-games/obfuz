using Obfuz.Emit;

namespace Obfuz.ObfusPasses
{
    public abstract class ObfuscationPassBase : IObfuscationPass
    {
        public abstract ObfuscationPassType Type { get; }

        public bool Support(ObfuscationPassType passType)
        {
            return passType.HasFlag(Type);
        }

        public abstract void Start(ObfuscationPassContext ctx);

        public abstract void Stop(ObfuscationPassContext ctx);

        public abstract void Process(ObfuscationPassContext ctx);
    }
}

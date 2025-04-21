namespace Obfuz
{
    public abstract class ObfuscationPassBase : IObfuscationPass
    {
        public virtual void Start(ObfuscatorContext ctx)
        {
        }

        public virtual void Stop(ObfuscatorContext ctx)
        {
        }
        public abstract void Process(ObfuscatorContext ctx);
    }
}

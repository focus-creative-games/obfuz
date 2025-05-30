using Obfuz.ObfusPasses;

namespace Obfuz
{
    public interface IObfuscationPass
    {
        ObfuscationPassType Type { get; }

        void Start();

        void Stop();

        void Process();
    }
}

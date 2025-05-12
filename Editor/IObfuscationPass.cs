using Obfuz.ObfusPasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IObfuscationPass
    {
        ObfuscationPassType Type { get; }

        void Start(ObfuscationPassContext ctx);

        void Stop(ObfuscationPassContext ctx);

        void Process(ObfuscationPassContext ctx);
    }
}

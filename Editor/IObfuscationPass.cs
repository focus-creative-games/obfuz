using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuz
{
    public interface IObfuscationPass
    {
        void Start(ObfuscatorContext ctx);

        void Stop(ObfuscatorContext ctx);

        void Process(ObfuscatorContext ctx);
    }
}

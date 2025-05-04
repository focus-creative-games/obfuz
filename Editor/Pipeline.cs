using Obfuz.ObfusPasses;
using System.Collections.Generic;

namespace Obfuz
{
    public class Pipeline
    {
        private readonly List<IObfuscationPass> _passes = new List<IObfuscationPass>();

        public Pipeline AddPass(IObfuscationPass pass)
        {
            _passes.Add(pass);
            return this;
        }

        public void Start(ObfuscationPassContext ctx)
        {
            foreach (var pass in _passes)
            {
                pass.Start(ctx);
            }
        }

        public void Stop(ObfuscationPassContext ctx)
        {

            foreach (var pass in _passes)
            {
                pass.Stop(ctx);
            }
        }

        public void Run(ObfuscationPassContext ctx)
        {
            foreach (var pass in _passes)
            {
                pass.Process(ctx);
            }
        }
    }
}

using System.Collections.Generic;

namespace Obfuz
{
    public class ObfuzPipeline
    {
        private readonly List<IObfuscationPass> _passes = new List<IObfuscationPass>();

        public ObfuzPipeline AddPass(IObfuscationPass pass)
        {
            _passes.Add(pass);
            return this;
        }

        public void Start(ObfuscatorContext ctx)
        {
            foreach (var pass in _passes)
            {
                pass.Start(ctx);
            }
        }

        public void Stop(ObfuscatorContext ctx)
        {

            foreach (var pass in _passes)
            {
                pass.Stop(ctx);
            }
        }

        public void Run(ObfuscatorContext ctx)
        {
            foreach (var pass in _passes)
            {
                pass.Process(ctx);
            }
        }
    }
}

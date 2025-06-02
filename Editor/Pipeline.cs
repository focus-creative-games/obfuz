using System.Collections.Generic;
using System.Diagnostics;

namespace Obfuz
{
    public class Pipeline
    {
        private readonly List<IObfuscationPass> _passes = new List<IObfuscationPass>();

        public bool Empty => _passes.Count == 0;

        public Pipeline AddPass(IObfuscationPass pass)
        {
            _passes.Add(pass);
            return this;
        }

        public void Start()
        {
            foreach (var pass in _passes)
            {
                pass.Start();
            }
        }

        public void Stop()
        {

            foreach (var pass in _passes)
            {
                pass.Stop();
            }
        }

        public void Run()
        {
            var sw = new Stopwatch();
            foreach (var pass in _passes)
            {
                sw.Restart();
                pass.Process();
                sw.Stop();
                UnityEngine.Debug.Log($"Pass: {pass.GetType().Name} process cost time: {sw.ElapsedMilliseconds}ms");
            }
        }
    }
}

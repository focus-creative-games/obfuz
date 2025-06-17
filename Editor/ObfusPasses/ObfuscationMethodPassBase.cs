using dnlib.DotNet;
using System.Linq;

namespace Obfuz.ObfusPasses
{
    public abstract class ObfuscationMethodPassBase : ObfuscationPassBase
    {
        protected virtual bool ForceProcessAllAssembliesAndIgnoreAllPolicy => false;

        protected abstract bool NeedObfuscateMethod(MethodDef method);

        protected abstract void ObfuscateData(MethodDef method);

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var modules = ForceProcessAllAssembliesAndIgnoreAllPolicy ? ctx.allObfuscationRelativeModules : ctx.modulesToObfuscate;
            ObfuscationMethodWhitelist whiteList = ctx.whiteList;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in modules)
            {
                if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && whiteList.IsInWhiteList(mod))
                {
                    continue;
                }
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes().ToArray())
                {
                    if (!ForceProcessAllAssembliesAndIgnoreAllPolicy && whiteList.IsInWhiteList(type))
                    {
                        continue;
                    }
                    // ToArray to avoid modify list exception
                    foreach (MethodDef method in type.Methods.ToArray())
                    {
                        if (!method.HasBody || (!ForceProcessAllAssembliesAndIgnoreAllPolicy && (ctx.whiteList.IsInWhiteList(method) || !Support(passPolicy.GetMethodObfuscationPasses(method)) || !NeedObfuscateMethod(method))))
                        {
                            continue;
                        }
                        // TODO if isGeneratedBy Obfuscator, continue
                        ObfuscateData(method);
                    }
                }
            }
        }
    }
}

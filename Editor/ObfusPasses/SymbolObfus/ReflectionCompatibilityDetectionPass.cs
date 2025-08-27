using dnlib.DotNet;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Collections.Generic;
using System.Threading;

namespace Obfuz.ObfusPasses.SymbolObfus
{
    public class ReflectionCompatibilityDetectionPass : ObfuscationPassBase
    {
        private readonly SymbolObfuscationSettingsFacade _settings;

        public override ObfuscationPassType Type => ObfuscationPassType.SymbolObfus;

        public ReflectionCompatibilityDetectionPass(SymbolObfuscationSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Start()
        {

        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var assemblyCache = ctx.assemblyCache;
            var toObfuscatedModules = ctx.modulesToObfuscate;
            var obfuscatedAndNotObfuscatedModules = ctx.allObfuscationRelativeModules;
            var toObfuscatedModuleSet = new HashSet<ModuleDef>(ctx.modulesToObfuscate);
            var renamePolicy = SymbolRename.CreateDefaultRenamePolicy(_settings.ruleFiles, _settings.customRenamePolicyTypes);
            var reflectionCompatibilityDetector = new ReflectionCompatibilityDetector(ctx.modulesToObfuscate, ctx.allObfuscationRelativeModules, renamePolicy);
            reflectionCompatibilityDetector.Analyze();
        }
    }
}

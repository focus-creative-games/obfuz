using dnlib.DotNet;
using Obfuz.Data;
using Obfuz.Emit;
using Obfuz.Settings;
using Obfuz.Utils;

namespace Obfuz.ObfusPasses.ControlFlowObfus
{
    class ObfusMethodContext
    {
        public MethodDef method;
        public LocalVariableAllocator localVariableAllocator;
        public IRandom localRandom;
        public EncryptionScopeInfo encryptionScope;
        public DefaultMetadataImporter importer;
        public ModuleConstFieldAllocator constFieldAllocator;
        public int minInstructionCountOfBasicBlockToObfuscate;

        public IRandom CreateRandom()
        {
            return encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
        }
    }

    internal class ControlFlowObfusPass : ObfuscationMethodPassBase
    {
        private readonly ControlFlowObfuscationSettingsFacade _settings;

        private IObfuscationPolicy _obfuscationPolicy;
        private IObfuscator _obfuscator;

        public ControlFlowObfusPass(ControlFlowObfuscationSettingsFacade settings)
        {
            _settings = settings;
            _obfuscator = new DefaultObfuscator();
        }

        public override ObfuscationPassType Type => ObfuscationPassType.ControlFlowObfus;

        public override void Start()
        {
            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            _obfuscationPolicy = new ConfigurableObfuscationPolicy(
                ctx.coreSettings.assembliesToObfuscate,
                _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        protected override bool NeedObfuscateMethod(MethodDef method)
        {
            return _obfuscationPolicy.NeedObfuscate(method);
        }

        protected override void ObfuscateData(MethodDef method)
        {
            //Debug.Log($"Obfuscating method: {method.FullName} with EvalStackObfusPass");

            ObfuscationPassContext ctx = ObfuscationPassContext.Current;
            var encryptionScope = ctx.encryptionScopeProvider.GetScope(method.Module);
            var ruleData = _obfuscationPolicy.GetObfuscationRuleData(method);
            var localRandom = encryptionScope.localRandomCreator(MethodEqualityComparer.CompareDeclaringTypes.GetHashCode(method));
            var obfusMethodCtx = new ObfusMethodContext
            {
                method = method,
                localVariableAllocator = new LocalVariableAllocator(method),
                encryptionScope = encryptionScope,
                constFieldAllocator = ctx.constFieldAllocator.GetModuleAllocator(method.Module),
                localRandom = localRandom,
                importer = ctx.moduleEntityManager.GetDefaultModuleMetadataImporter(method.Module, ctx.encryptionScopeProvider),
                minInstructionCountOfBasicBlockToObfuscate = _settings.minInstructionCountOfBasicBlockToObfuscate,
            };
            _obfuscator.Obfuscate(method, obfusMethodCtx);
        }
    }
}

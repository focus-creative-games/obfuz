using dnlib.DotNet;
using Obfuz.Settings;
using Obfuz.Utils;
using System.Linq;

namespace Obfuz.ObfusPasses.RemoveConstField
{

    public class RemoveConstFieldPass : ObfuscationPassBase
    {
        private RemoveConstFieldSettingsFacade _settings;
        private ObfuzIgnoreScopeComputeCache _obfuzIgnoreScopeComputeCache;
        private IRemoveConstFieldPolicy _removeConstFieldPolicy;

        public override ObfuscationPassType Type => ObfuscationPassType.RemoveConstField;

        public RemoveConstFieldPass(RemoveConstFieldSettingsFacade settings)
        {
            _settings = settings;
        }

        public override void Start()
        {
            var ctx = ObfuscationPassContext.Current;
            _obfuzIgnoreScopeComputeCache = ctx.obfuzIgnoreScopeComputeCache;
            _removeConstFieldPolicy = new ConfigurableRemoveConstFieldPolicy(ctx.coreSettings.assembliesToObfuscate, _settings.ruleFiles);
        }

        public override void Stop()
        {

        }

        public override void Process()
        {
            var ctx = ObfuscationPassContext.Current;
            var modules = ctx.modulesToObfuscate;
            ConfigurablePassPolicy passPolicy = ctx.passPolicy;
            foreach (ModuleDef mod in modules)
            {
                // ToArray to avoid modify list exception
                foreach (TypeDef type in mod.GetTypes())
                {
                    if (type.IsEnum)
                    {
                        continue;
                    }
                    foreach (FieldDef field in type.Fields.ToArray())
                    {
                        if (!field.IsLiteral)
                        {
                            continue;
                        }
                        if (!Support(passPolicy.GetFieldObfuscationPasses(field)))
                        {
                            continue;
                        }
                        if (_obfuzIgnoreScopeComputeCache.HasSelfOrDeclaringOrEnclosingOrInheritObfuzIgnoreScope(field, field.DeclaringType, ObfuzScope.Field))
                        {
                            continue;
                        }
                        if (_removeConstFieldPolicy.NeedPreserved(field))
                        {
                            continue;
                        }
                        field.DeclaringType = null;
                        //Debug.Log($"Remove const field {field.FullName} in type {type.FullName} in module {mod.Name}");
                    }
                }
            }
        }
    }
}

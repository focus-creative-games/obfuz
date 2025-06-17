using dnlib.DotNet;
using Obfuz.Conf;
using Obfuz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Obfuz.ObfusPasses.ExprObfus
{

    public interface IObfuscationPolicy
    {
        bool NeedObfuscate(MethodDef method);
    }

    public abstract class ObfuscationPolicyBase : IObfuscationPolicy
    {
        public abstract bool NeedObfuscate(MethodDef method);
    }

    public class ConfigurableObfuscationPolicy : ObfuscationPolicyBase
    {
        class ObfuscationRule : IRule<ObfuscationRule>
        {
            public bool? obfuscate;

            public void InheritParent(ObfuscationRule parentRule)
            {
                if (obfuscate == null)
                    obfuscate = parentRule.obfuscate;
            }
        }

        class MethodSpec : MethodRuleBase<ObfuscationRule>
        {
        }

        class TypeSpec : TypeRuleBase<MethodSpec, ObfuscationRule>
        {
        }

        class AssemblySpec : AssemblyRuleBase<TypeSpec, MethodSpec, ObfuscationRule>
        {
        }

        private static readonly ObfuscationRule s_default = new ObfuscationRule()
        {
            obfuscate = false,
        };

        private readonly XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule> _xmlParser;

        private readonly Dictionary<MethodDef, ObfuscationRule> _methodRuleCache = new Dictionary<MethodDef, ObfuscationRule>();

        public ConfigurableObfuscationPolicy(List<string> toObfuscatedAssemblyNames, List<string> xmlConfigFiles)
        {
            _xmlParser = new XmlAssemblyTypeMethodRuleParser<AssemblySpec, TypeSpec, MethodSpec, ObfuscationRule>(
                toObfuscatedAssemblyNames, ParseObfuscationRule, null);
            LoadConfigs(xmlConfigFiles);
        }

        private void LoadConfigs(List<string> configFiles)
        {
            _xmlParser.LoadConfigs(configFiles);
            _xmlParser.InheritParentRules(s_default);
        }

        private ObfuscationRule ParseObfuscationRule(string configFile, XmlElement ele)
        {
            var rule = new ObfuscationRule();
            if (ele.HasAttribute("obfuscate"))
            {
                rule.obfuscate = ConfigUtil.ParseBool(ele.GetAttribute("obfuscate"));
            }
            return rule;
        }

        private ObfuscationRule GetMethodObfuscationRule(MethodDef method)
        {
            if (!_methodRuleCache.TryGetValue(method, out var rule))
            {
                rule = _xmlParser.GetMethodRule(method, s_default);
                _methodRuleCache[method] = rule;
            }
            return rule;
        }

        public override bool NeedObfuscate(MethodDef method)
        {
            ObfuscationRule rule = GetMethodObfuscationRule(method);
            return rule.obfuscate == true;
        }
    }
}
